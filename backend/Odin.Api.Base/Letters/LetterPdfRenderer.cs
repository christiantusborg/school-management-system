using System.Globalization;
using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Odin.Api.Base.Letters;

public sealed class LetterPdfRenderer
{
    /// <summary>
    /// Renders a coordinate-based certificate: a background image fills the
    /// page, and every <see cref="CertificateField"/> in the layout is drawn
    /// on top at its absolute (X, Y). Pixel coordinates from the design map
    /// 1:1 onto QuestPDF points so the visual editor stays WYSIWYG.
    /// </summary>
    public byte[] RenderCertificate(
        CertificateLayout layout,
        IReadOnlyDictionary<Guid, byte[]> assetBytes,
        IReadOnlyDictionary<string, string> tagValues,
        IReadOnlyList<TranscriptGradeRow>? transcriptRows = null,
        string? watermark = null)
    {
        var lw = layout.Width <= 0 ? 2000 : layout.Width;
        var lh = layout.Height <= 0 ? 1414 : layout.Height;
        var assets = assetBytes ?? new Dictionary<Guid, byte[]>();

        // Output page size: explicit pageSize + orientation if provided,
        // else default A4 with auto orientation (landscape iff canvas wider
        // than tall).
        var pageSize = ResolvePageSize(layout, lw, lh);
        var pw = pageSize.Width;
        var ph = pageSize.Height;
        var sx = pw / (float)lw;
        var sy = ph / (float)lh;

        var allRows = transcriptRows ?? Array.Empty<TranscriptGradeRow>();

        return Document.Create(container =>
        {
            foreach (var pageDef in layout.GetPages())
            {
                // Legacy auto-paginating grade table (no range): render the page
                // as a flowing Header/Content/Footer page so the table paginates
                // across as many pages as needed. A page using ranged tables is
                // instead rendered normally with each range as a fixed slice.
                var flowingField = (pageDef.Fields ?? new()).FirstOrDefault(f =>
                    string.Equals((f.Kind ?? "text").Trim(), "transcriptTable", StringComparison.OrdinalIgnoreCase)
                    && f.RowEnd <= 0);
                if (flowingField is not null)
                {
                    RenderFlowingTranscriptPage(container, pageDef, flowingField, pageSize, sx, sy, lw, lh,
                        tagValues, assets, allRows, watermark);
                    continue;
                }

                container.Page(page =>
                {
                    page.Size(pageSize);
                    page.Margin(0);
                    page.PageColor(Colors.White);
                    ApplyWatermark(page, watermark);

                    page.Content().Layers(layers =>
                    {
                        var bgBytes = pageDef.BackgroundAssetId is { } bgId && assets.TryGetValue(bgId, out var bg)
                            ? bg
                            : Array.Empty<byte>();
                        if (bgBytes is { Length: > 0 })
                            // FitArea (not the implicit native-pixel size) so a
                            // 2000×1414 source PNG scales down onto an A4 page
                            // instead of forcing QuestPDF into a "conflicting
                            // size constraints" exception.
                            layers.PrimaryLayer().Image(bgBytes).FitArea();
                        else
                            layers.PrimaryLayer().Background(Colors.White);

                        // Auto Total/GPA: unless the layout has a manually-placed
                        // transcriptTotals field, the Total + GPA rows are appended
                        // directly under the last grade-range table that holds data
                        // (the range containing the final grade row, else the
                        // highest range). This keeps the total glued to the end of
                        // the grades no matter how many ranges are populated.
                        var pageFields = pageDef.Fields ?? new();
                        var hasManualTotals = pageFields.Any(f =>
                            string.Equals((f.Kind ?? "").Trim(), "transcriptTotals", StringComparison.OrdinalIgnoreCase));
                        var schoolName = tagValues.TryGetValue("[school name]", out var snv) ? snv : null;
                        string? autoTotalsFieldId = null;
                        if (!hasManualTotals && allRows.Count > 0)
                        {
                            var ranges = pageFields
                                .Where(f => string.Equals((f.Kind ?? "").Trim(), "transcriptTable", StringComparison.OrdinalIgnoreCase) && f.RowEnd > 0)
                                .OrderBy(f => f.RowEnd).ToList();
                            var containing = ranges.FirstOrDefault(f => Math.Max(1, f.RowStart) <= allRows.Count && allRows.Count <= f.RowEnd);
                            autoTotalsFieldId = (containing ?? ranges.LastOrDefault())?.Id;
                        }

                        foreach (var field in pageDef.Fields ?? new())
                        {
                            var kind = (field.Kind ?? "text").Trim().ToLowerInvariant();
                            if (kind == "transcripttable")
                            {
                                if (field.RowEnd > 0)
                                {
                                    // Ranged slice: rows [RowStart..RowEnd] (1-based).
                                    var from = Math.Max(1, field.RowStart);
                                    var to = Math.Max(from, field.RowEnd);
                                    var slice = allRows.Skip(from - 1).Take(to - from + 1).ToList();
                                    RenderTranscriptTableSlice(layers, field, sx, sy, slice,
                                        field.Id == autoTotalsFieldId ? allRows : null, schoolName);
                                }
                                else
                                {
                                    RenderTranscriptTable(layers, field, sx, sy, allRows, schoolName);
                                }
                                continue;
                            }
                            if (kind == "transcripttotals")
                            {
                                RenderTranscriptTotals(layers, field, sx, sy, allRows, schoolName);
                                continue;
                            }
                            if (kind == "gradestandardtable")
                            {
                                RenderGradeStandardTable(layers, field, sx, sy, schoolName);
                                continue;
                            }
                            if (kind == "image")
                            {
                                if (field.ImageAssetId is not { } id || !assets.TryGetValue(id, out var bytes) || bytes is null || bytes.Length == 0)
                                    continue;
                                var fw = (field.Width  <= 0 ? 200 : field.Width)  * sx;
                                var fh = (field.Height <= 0 ? 200 : field.Height) * sy;
                                layers.Layer()
                                    .PaddingTop((float)field.Y * sy)
                                    .PaddingLeft((float)field.X * sx)
                                    .Width(fw)
                                    .Height(fh)
                                    .Image(bytes).FitArea();
                                continue;
                            }

                            var text = ResolveFieldText(field, tagValues);
                            if (string.IsNullOrWhiteSpace(text)) continue;

                            var l = layers.Layer();
                            var hasExplicitWidth = field.Width > 0;
                            var align = field.Align?.Trim().ToLowerInvariant();

                            // Two layout modes:
                            //  1) Width > 0 → field.X is the LEFT edge of a box of
                            //     width=field.Width; alignment applies WITHIN that box;
                            //     long text wraps inside.
                            //  2) Width == 0 (legacy) → center spans full page width
                            //     with field.X ignored as anchor; right uses field.X as
                            //     the right edge anchor; left positions at field.X.
                            QuestPDF.Infrastructure.IContainer aligned;
                            if (hasExplicitWidth)
                            {
                                var boxW = field.Width * sx;
                                aligned = l
                                    .PaddingTop((float)field.Y * sy)
                                    .PaddingLeft((float)field.X * sx)
                                    .Width(boxW);
                                aligned = align switch
                                {
                                    "center" => aligned.AlignCenter(),
                                    "right"  => aligned.AlignRight(),
                                    _        => aligned.AlignLeft(),
                                };
                            }
                            else
                            {
                                aligned = align switch
                                {
                                    "center" => l.PaddingTop((float)field.Y * sy).Width(pw).AlignCenter(),
                                    "right"  => l.PaddingTop((float)field.Y * sy).Width((float)field.X * sx).AlignRight(),
                                    _        => l.PaddingTop((float)field.Y * sy).PaddingLeft((float)field.X * sx),
                                };
                            }

                            var fontSize = Math.Max(4f, field.FontSize * sy);
                            var color = string.IsNullOrWhiteSpace(field.Color) ? "#000000" : field.Color;

                            if (!string.IsNullOrWhiteSpace(field.HtmlText) && field.Tag is null)
                            {
                                // Static field with rich HTML body. Renders as a
                                // Column of block-level items so tables/hr can
                                // sit alongside paragraphs (which Text() can't host).
                                aligned.Element(c =>
                                    RenderHtmlBlocks(c, field.HtmlText!, fontSize, color, field.Bold, field.Italic));
                            }
                            else
                            {
                                aligned.Text(t =>
                                {
                                    var span = t.Span(text);
                                    span.FontSize(fontSize);
                                    span.FontColor(color);
                                    if (field.Bold) span.Bold();
                                    if (field.Italic) span.Italic();
                                });
                            }
                        }
                    });
                });
            }
        }).GeneratePdf();
    }

    /// <summary>
    /// Overlays a faint diagonal watermark (e.g. "PROVISIONAL") across the whole
    /// page foreground when <paramref name="text"/> is set. Used to mark the
    /// draft transcript a student can download mid-grading so it can't be
    /// mistaken for the official one. No-op when null/blank.
    /// </summary>
    private static void ApplyWatermark(PageDescriptor page, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        // 30% alpha (70% transparent) so the transcript content stays readable
        // underneath — solid Red.Lighten3 hid the rows it crossed.
        page.Foreground()
            .AlignCenter()
            .AlignMiddle()
            .Rotate(-35)
            .Text(text!)
            .FontSize(70).Bold().FontColor(Color.FromHex("#4DEF9A9A"));
    }

    /// <summary>
    /// Renders the transcript page with the grades table in a flowing
    /// Content region between a repeated Header (letterhead + student details)
    /// and a repeated Footer (accreditation band). The table paginates onto as
    /// many pages as needed, repeating its column header; the signature block
    /// flows in after the last grade row. Zones are derived from the stored
    /// field geometry: everything above the table is the header band, the bottom
    /// accreditation image(s) are the footer band, and fields between the table
    /// and the footer (signature/stamp) flow after the table.
    /// </summary>
    private void RenderFlowingTranscriptPage(
        IDocumentContainer container,
        CertificatePage pageDef,
        CertificateField tableField,
        PageSize pageSize,
        float sx, float sy,
        int canvasW, int canvasH,
        IReadOnlyDictionary<string, string> tagValues,
        IReadOnlyDictionary<Guid, byte[]> assets,
        IReadOnlyList<TranscriptGradeRow> rows,
        string? watermark = null)
    {
        var pw = pageSize.Width;
        var fields = pageDef.Fields ?? new();
        var tableTopY = (float)tableField.Y;

        static bool IsTable(CertificateField f)
        {
            var k = (f.Kind ?? "text").Trim().ToLowerInvariant();
            return k == "transcripttable" || k == "gradestandardtable";
        }

        // Footer band = the bottom accreditation image(s): a tall image field in
        // the lower half of the canvas. Thin divider lines (height 2) and text
        // are excluded so they stay with the signature block.
        var footerFields = fields.Where(f =>
            string.Equals((f.Kind ?? "").Trim(), "image", StringComparison.OrdinalIgnoreCase)
            && f.Height >= 30 && f.Y > canvasH * 0.5).ToList();
        var footerTopY = footerFields.Count > 0 ? (float)footerFields.Min(f => f.Y) : (float)canvasH;

        var headerFields = fields.Where(f => f.Y < tableTopY && !IsTable(f)).ToList();
        var afterFields = fields.Where(f => f.Y >= tableTopY && f.Y < footerTopY && !IsTable(f)).ToList();

        var bgBytes = pageDef.BackgroundAssetId is { } bgId && assets.TryGetValue(bgId, out var bg)
            ? bg : Array.Empty<byte>();
        var tableFontSize = Math.Max(6f, tableField.FontSize <= 0 ? 9f : tableField.FontSize * sy);
        var headerHeight = tableTopY * sy;
        var footerHeight = ((float)canvasH - footerTopY) * sy;

        container.Page(page =>
        {
            page.Size(pageSize);
            page.Margin(0);
            page.PageColor(Colors.White);
            if (bgBytes is { Length: > 0 }) page.Background().Image(bgBytes).FitArea();
            ApplyWatermark(page, watermark);

            page.Header().Height(headerHeight).Layers(layers =>
            {
                layers.PrimaryLayer().Background(Colors.Transparent);
                foreach (var f in headerFields) RenderFieldLayer(layers, f, sx, sy, tagValues, assets, pw, 0f);
            });

            if (footerFields.Count > 0)
            {
                page.Footer().Height(footerHeight).Layers(layers =>
                {
                    layers.PrimaryLayer().Background(Colors.Transparent);
                    foreach (var f in footerFields) RenderFieldLayer(layers, f, sx, sy, tagValues, assets, pw, footerTopY * sy);
                });
            }

            page.Content().PaddingTop(6).Column(col =>
            {
                col.Item()
                   .PaddingLeft((float)tableField.X * sx)
                   .Width((tableField.Width <= 0 ? 500 : tableField.Width) * sx)
                   .Element(c => BuildTranscriptTableContent(c, tableField, rows, tableFontSize,
                       tagValues.TryGetValue("[school name]", out var flowSchool) ? flowSchool : null));

                if (afterFields.Count > 0)
                {
                    var sigTop = (float)afterFields.Min(f => f.Y);
                    var sigBottom = (float)afterFields.Max(f => f.Y);
                    var sigHeight = (sigBottom - sigTop + 30) * sy;
                    col.Item().PaddingTop(24).Height(sigHeight).Layers(layers =>
                    {
                        layers.PrimaryLayer().Background(Colors.Transparent);
                        foreach (var f in afterFields) RenderFieldLayer(layers, f, sx, sy, tagValues, assets, pw, sigTop * sy);
                    });
                }
            });
        });
    }

    /// <summary>
    /// Renders a single text or image field into a layers container at its
    /// absolute (x, y), minus <paramref name="offsetYpx"/> so header/footer/
    /// signature bands can re-base their fields to the band's top. Mirrors the
    /// absolute-field rendering used for non-flowing pages.
    /// </summary>
    private static void RenderFieldLayer(
        LayersDescriptor layers,
        CertificateField field,
        float sx, float sy,
        IReadOnlyDictionary<string, string> tagValues,
        IReadOnlyDictionary<Guid, byte[]> assets,
        float pageWidth,
        float offsetYpx)
    {
        var kind = (field.Kind ?? "text").Trim().ToLowerInvariant();
        var topPx = Math.Max(0f, (float)field.Y * sy - offsetYpx);

        if (kind == "image")
        {
            if (field.ImageAssetId is not { } id || !assets.TryGetValue(id, out var bytes) || bytes is null || bytes.Length == 0)
                return;
            var fw = (field.Width <= 0 ? 200 : field.Width) * sx;
            var fh = (field.Height <= 0 ? 200 : field.Height) * sy;
            layers.Layer()
                .PaddingTop(topPx)
                .PaddingLeft((float)field.X * sx)
                .Width(fw).Height(fh)
                .Image(bytes).FitArea();
            return;
        }

        var textVal = ResolveFieldText(field, tagValues);
        if (string.IsNullOrWhiteSpace(textVal)) return;

        var l = layers.Layer();
        var hasExplicitWidth = field.Width > 0;
        var align = field.Align?.Trim().ToLowerInvariant();
        IContainer aligned;
        if (hasExplicitWidth)
        {
            aligned = l.PaddingTop(topPx).PaddingLeft((float)field.X * sx).Width(field.Width * sx);
            aligned = align switch
            {
                "center" => aligned.AlignCenter(),
                "right"  => aligned.AlignRight(),
                _        => aligned.AlignLeft(),
            };
        }
        else
        {
            aligned = align switch
            {
                "center" => l.PaddingTop(topPx).Width(pageWidth).AlignCenter(),
                "right"  => l.PaddingTop(topPx).Width((float)field.X * sx).AlignRight(),
                _        => l.PaddingTop(topPx).PaddingLeft((float)field.X * sx),
            };
        }

        var fontSize = Math.Max(4f, field.FontSize * sy);
        var color = string.IsNullOrWhiteSpace(field.Color) ? "#000000" : field.Color;
        if (!string.IsNullOrWhiteSpace(field.HtmlText) && field.Tag is null)
            aligned.Element(c => RenderHtmlBlocks(c, field.HtmlText!, fontSize, color, field.Bold, field.Italic));
        else
            aligned.Text(t =>
            {
                var span = t.Span(textVal);
                span.FontSize(fontSize);
                span.FontColor(color);
                if (field.Bold) span.Bold();
                if (field.Italic) span.Italic();
            });
    }

    /// <summary>
    /// Builds the grades table as flowing content (not a fixed layer). The
    /// column header repeats on every page via <c>table.Header</c>; the Total
    /// and GPA rows render once after the last grade row.
    /// </summary>
    private static void BuildTranscriptTableContent(
        IContainer container,
        CertificateField field,
        IReadOnlyList<TranscriptGradeRow> rows,
        float fontSize,
        string? schoolName = null)
    {
        var colDefs = SelectTranscriptCols(field, schoolName);
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                foreach (var cd in colDefs) cols.RelativeColumn(cd.Width);
            });

            table.Header(header =>
            {
                foreach (var cd in colDefs)
                    header.Cell().Border(0.5f).BorderColor(Colors.Black)
                        .Padding(3).AlignCenter().AlignMiddle().Text(cd.Header).Bold().FontSize(fontSize);
            });

            void Cell(string text, bool left = false, bool bold = false)
            {
                var c = table.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).MinHeight(16);
                var span = (left ? c.AlignLeft() : c.AlignCenter()).AlignMiddle().Text(text).FontSize(fontSize);
                if (bold) span.Bold();
            }

            decimal totalEcts = 0m;
            double totalGp = 0;
            foreach (var r in rows)
            {
                var (ects, uk, gp) = MapScore(r.Score);
                var pts = (double)r.Ects * gp;
                totalEcts += r.Ects;
                totalGp += pts;
                foreach (var cd in colDefs)
                    Cell(cd.Compute(r, (ects, uk, gp, pts)), left: cd.AlignLeft);
            }

            var gpa = totalEcts > 0 ? totalGp / (double)totalEcts : 0;
            RenderTranscriptTotalsRows(table, colDefs, totalEcts, totalGp, gpa, fontSize);
        });
    }

    // A selectable transcript grade-table column: header text, relative width,
    // left/centre alignment, and how to render a row's cell. `Compute` receives
    // the row plus its derived (ectsGrade, ibssGrade, ectsGradePoint, gradePoint).
    private sealed record TranscriptCol(
        string Key, string Header, int Width, bool AlignLeft,
        Func<TranscriptGradeRow, (string EctsGrade, string Ibss, double Gp, double Pts), string> Compute);

    private static readonly TranscriptCol[] AllTranscriptCols =
    {
        new("code",           "Code",               2, false, (r, d) => r.Code),
        new("module",         "Module",             4, true,  (r, d) => r.Name),
        new("ects",           "ECTS\ncredit hours", 2, false, (r, d) => r.Ects.ToString("0.##", CultureInfo.InvariantCulture)),
        new("ectsGrade",      "ECTS\nGrade",        2, false, (r, d) => d.EctsGrade),
        // The school's own grade = the numeric score the grader entered. Header
        // is rewritten per-render to "{School} Grade" (see SelectTranscriptCols).
        new("ibssGrade",      "School\nGrade",      2, false, (r, d) => r.Score.ToString("0.##", CultureInfo.InvariantCulture)),
        new("ectsGradePoint", "ECTS\nGrade Point",  2, false, (r, d) => d.Gp.ToString("0.0", CultureInfo.InvariantCulture)),
        new("gradePoint",     "Grade\nPoint",       2, false, (r, d) => d.Pts.ToString("0.0", CultureInfo.InvariantCulture)),
    };

    // Resolve which columns a field renders (null/empty/unknown => all), with the
    // school-grade column's header set to "{School} Grade" for this enrolment.
    private static IReadOnlyList<TranscriptCol> SelectTranscriptCols(CertificateField field, string? schoolName = null)
    {
        var keys = field.Columns;
        var baseCols = (keys is null || keys.Count == 0)
            ? AllTranscriptCols.AsEnumerable()
            : AllTranscriptCols.Where(c =>
                keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase).Contains(c.Key));
        var chosen = baseCols.ToList();
        if (chosen.Count == 0) chosen = AllTranscriptCols.ToList();

        // Fallback is the neutral "School" (never the "MGW" portal brand) so a
        // programme with no awarding school still reads "School Grade" on the
        // document, per the IBAS/IBSS-or-School-grade rule.
        var school = string.IsNullOrWhiteSpace(schoolName) ? "School" : schoolName.Trim();
        return chosen.Select(c => c.Key == "ibssGrade"
            ? c with { Header = $"{school}\nGrade" }
            : c).ToList();
    }

    // Renders the Total and Grade-Point-Average rows across the selected columns.
    // The ECTS total lands under the "ects" column and the grade-point total /
    // GPA under the grade-point column, so they stay aligned whatever the author
    // chose. Uses per-column cells (no ColumnSpan) to survive arbitrary layouts.
    private static void RenderTranscriptTotalsRows(
        QuestPDF.Fluent.TableDescriptor table, IReadOnlyList<TranscriptCol> cols,
        decimal totalEcts, double totalGp, double gpa, float fontSize)
    {
        var gpKey = cols.Any(c => c.Key == "gradePoint") ? "gradePoint"
            : cols.Any(c => c.Key == "ectsGradePoint") ? "ectsGradePoint"
            : cols[^1].Key;

        void Cell(string text, bool left, bool bold)
        {
            var cell = table.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).MinHeight(16);
            var span = (left ? cell.AlignLeft() : cell.AlignCenter()).AlignMiddle().Text(text).FontSize(fontSize);
            if (bold) span.Bold();
        }

        bool labelDone = false;
        foreach (var c in cols)
        {
            if (!labelDone) { Cell("Total", left: true, bold: true); labelDone = true; }
            else if (c.Key == "ects") Cell(totalEcts.ToString("0.##", CultureInfo.InvariantCulture), left: false, bold: true);
            else if (c.Key == gpKey) Cell(totalGp.ToString("0.0", CultureInfo.InvariantCulture), left: false, bold: true);
            else Cell("", left: false, bold: false);
        }

        labelDone = false;
        foreach (var c in cols)
        {
            if (!labelDone) { Cell("Grade Point Average", left: true, bold: true); labelDone = true; }
            else if (c.Key == gpKey) Cell(gpa.ToString("0.00", CultureInfo.InvariantCulture), left: false, bold: true);
            else Cell("", left: false, bold: false);
        }
    }

    private static void RenderTranscriptTable(
        QuestPDF.Fluent.LayersDescriptor layers,
        CertificateField field,
        float sx, float sy,
        IReadOnlyList<TranscriptGradeRow> rows,
        string? schoolName = null)
    {
        var x = (float)field.X * sx;
        var y = (float)field.Y * sy;
        var w = Math.Max(50f, (float)(field.Width <= 0 ? 800 : field.Width) * sx);
        var fontSize = Math.Max(6f, field.FontSize <= 0 ? 9f : field.FontSize * sy);

        var colDefs = SelectTranscriptCols(field, schoolName);
        layers.Layer().PaddingTop(y).PaddingLeft(x).Width(w).Element(c =>
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    foreach (var cd in colDefs) cols.RelativeColumn(cd.Width);
                });

                foreach (var cd in colDefs) Header(table, cd.Header);

                decimal totalEcts = 0m;
                double totalGp = 0;
                foreach (var row in rows)
                {
                    var (ects, uk, gp) = MapScore(row.Score);
                    var pts = (double)row.Ects * gp;
                    totalEcts += row.Ects;
                    totalGp += pts;
                    foreach (var cd in colDefs)
                        Cell(table, cd.Compute(row, (ects, uk, gp, pts)), fontSize, alignLeft: cd.AlignLeft);
                }

                // Pad with blank rows so the table looks like the reference (10 rows).
                var minRows = 10;
                for (int i = rows.Count; i < minRows; i++)
                    foreach (var cd in colDefs) Cell(table, "", fontSize);

                var gpa = totalEcts > 0 ? totalGp / (double)totalEcts : 0;
                RenderTranscriptTotalsRows(table, colDefs, totalEcts, totalGp, gpa, fontSize);
            });
        });

        static void Header(QuestPDF.Fluent.TableDescriptor t, string text)
        {
            t.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).AlignCenter().AlignMiddle().Text(text).Bold().FontSize(9);
        }
        static void Cell(QuestPDF.Fluent.TableDescriptor t, string text, float fontSize, bool alignLeft = false, bool bold = false)
        {
            var cell = t.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).MinHeight(16);
            var aligned = alignLeft ? cell.AlignLeft() : cell.AlignCenter();
            var span = aligned.AlignMiddle().Text(text).FontSize(fontSize);
            if (bold) span.Bold();
        }
    }

    /// <summary>
    /// Renders one fixed range of the grades table at the field's position
    /// (e.g. rows 1-10, 11-20). Shows the column header and the given slice. When
    /// <paramref name="totalsOverAllRows"/> is non-null, the Total + GPA rows
    /// (computed over ALL grades) are appended directly below the slice — used
    /// to glue the total to the last populated range table.
    /// </summary>
    private static void RenderTranscriptTableSlice(
        QuestPDF.Fluent.LayersDescriptor layers,
        CertificateField field,
        float sx, float sy,
        IReadOnlyList<TranscriptGradeRow> slice,
        IReadOnlyList<TranscriptGradeRow>? totalsOverAllRows = null,
        string? schoolName = null)
    {
        var x = (float)field.X * sx;
        var y = (float)field.Y * sy;
        var w = Math.Max(50f, (float)(field.Width <= 0 ? 800 : field.Width) * sx);
        var fontSize = Math.Max(6f, field.FontSize <= 0 ? 9f : field.FontSize * sy);

        var colDefs = SelectTranscriptCols(field, schoolName);
        layers.Layer().PaddingTop(y).PaddingLeft(x).Width(w).Element(c =>
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    foreach (var cd in colDefs) cols.RelativeColumn(cd.Width);
                });

                foreach (var cd in colDefs) Header(table, cd.Header);

                foreach (var row in slice)
                {
                    var (ects, uk, gp) = MapScore(row.Score);
                    var pts = (double)row.Ects * gp;
                    foreach (var cd in colDefs)
                        Cell(table, cd.Compute(row, (ects, uk, gp, pts)), fontSize, alignLeft: cd.AlignLeft);
                }

                // Only completed subjects are rendered — no empty padding rows.
                // A range table with 5 graded subjects shows exactly 5 rows.

                // Auto Total/GPA glued under the last populated range table.
                if (totalsOverAllRows is not null)
                {
                    decimal totalEcts = 0m;
                    double totalGp = 0;
                    foreach (var r in totalsOverAllRows)
                    {
                        var (_, _, gp) = MapScore(r.Score);
                        totalEcts += r.Ects;
                        totalGp += (double)r.Ects * gp;
                    }
                    var gpa = totalEcts > 0 ? totalGp / (double)totalEcts : 0;
                    RenderTranscriptTotalsRows(table, colDefs, totalEcts, totalGp, gpa, fontSize);
                }
            });
        });

        static void Header(QuestPDF.Fluent.TableDescriptor t, string text)
            => t.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).AlignCenter().AlignMiddle().Text(text).Bold().FontSize(9);
        static void Cell(QuestPDF.Fluent.TableDescriptor t, string text, float fontSize, bool alignLeft = false, bool bold = false)
        {
            var cell = t.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).MinHeight(16);
            var span = (alignLeft ? cell.AlignLeft() : cell.AlignCenter()).AlignMiddle().Text(text).FontSize(fontSize);
            if (bold) span.Bold();
        }
    }

    /// <summary>
    /// Renders the standalone Total + Grade Point Average summary (a
    /// transcriptTotals field) computed over all grades, at its own position so
    /// admins can place it as its own section independent of the grade tables.
    /// </summary>
    private static void RenderTranscriptTotals(
        QuestPDF.Fluent.LayersDescriptor layers,
        CertificateField field,
        float sx, float sy,
        IReadOnlyList<TranscriptGradeRow> allRows,
        string? schoolName = null)
    {
        var x = (float)field.X * sx;
        var y = (float)field.Y * sy;
        var w = Math.Max(50f, (float)(field.Width <= 0 ? 800 : field.Width) * sx);
        var fontSize = Math.Max(6f, field.FontSize <= 0 ? 9f : field.FontSize * sy);

        decimal totalEcts = 0m;
        double totalGp = 0;
        foreach (var row in allRows)
        {
            var (_, _, gp) = MapScore(row.Score);
            totalEcts += row.Ects;
            totalGp += (double)row.Ects * gp;
        }
        var gpa = totalEcts > 0 ? totalGp / (double)totalEcts : 0;
        var colDefs = SelectTranscriptCols(field, schoolName);

        layers.Layer().PaddingTop(y).PaddingLeft(x).Width(w).Element(c =>
        {
            c.Table(table =>
            {
                // Mirror the grade table's column grid so Total/GPA line up with
                // the table above it (pick the same columns on both fields).
                table.ColumnsDefinition(cols =>
                {
                    foreach (var cd in colDefs) cols.RelativeColumn(cd.Width);
                });

                RenderTranscriptTotalsRows(table, colDefs, totalEcts, totalGp, gpa, fontSize);
            });
        });
    }

    /// <summary>
    /// Block-level renderer for rich HTML inside a static field. Each top-level
    /// child of the parsed HTML becomes a Column item: paragraphs/headings as
    /// Text-with-spans, lists as inner Columns, &lt;hr&gt; as a horizontal line,
    /// and &lt;table&gt; as a QuestPDF Table. Inline runs (bold/italic/underline/
    /// strike/color/highlight) reuse <see cref="WalkRich"/>.
    /// </summary>
    private static void RenderHtmlBlocks(
        QuestPDF.Infrastructure.IContainer container,
        string html,
        float fontSize,
        string color,
        bool boldBase,
        bool italicBase)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.LoadHtml($"<root>{html}</root>");
        var root = doc.DocumentNode.SelectSingleNode("//root") ?? doc.DocumentNode;

        container.Column(col =>
        {
            col.Spacing(2);
            foreach (var node in root.ChildNodes) RenderBlock(col, node, fontSize, color, boldBase, italicBase);
        });
    }

    private static void RenderBlock(QuestPDF.Fluent.ColumnDescriptor col, HtmlAgilityPack.HtmlNode node,
        float fontSize, string color, bool bold, bool italic)
    {
        // Bare text nodes at the block level → wrap in a paragraph.
        if (node.NodeType == HtmlAgilityPack.HtmlNodeType.Text)
        {
            var text = System.Net.WebUtility.HtmlDecode(node.InnerText);
            if (!string.IsNullOrWhiteSpace(text))
            {
                col.Item().Text(t =>
                {
                    var span = t.Span(text).FontSize(fontSize).FontColor(color);
                    if (bold) span.Bold();
                    if (italic) span.Italic();
                });
            }
            return;
        }
        if (node.NodeType != HtmlAgilityPack.HtmlNodeType.Element) return;

        var name = node.Name.ToLowerInvariant();
        switch (name)
        {
            case "p":
            case "div":
            case "h1":
            case "h2":
            case "h3":
            {
                var nfs = fontSize;
                var nb = bold;
                if (name == "h1") { nfs = fontSize * 1.6f; nb = true; }
                else if (name == "h2") { nfs = fontSize * 1.35f; nb = true; }
                else if (name == "h3") { nfs = fontSize * 1.15f; nb = true; }
                var (alignClass, blockColor) = ParseBlockStyle(node, color);
                var item = col.Item();
                if (alignClass == "center") item = item.AlignCenter();
                else if (alignClass == "right") item = item.AlignRight();
                item.Text(t => WalkRich(t, node, nfs, blockColor, nb, italic, underline: false, strike: false, highlight: null));
                break;
            }
            case "ul":
            case "ol":
            {
                var ordered = name == "ol";
                var idx = 1;
                col.Item().Column(inner =>
                {
                    foreach (var li in node.ChildNodes)
                    {
                        if (li.NodeType != HtmlAgilityPack.HtmlNodeType.Element) continue;
                        if (!string.Equals(li.Name, "li", StringComparison.OrdinalIgnoreCase)) continue;
                        var prefix = ordered ? $"{idx++}. " : "•  ";
                        inner.Item().Row(r =>
                        {
                            r.ConstantItem(fontSize * 1.4f).Text(prefix).FontSize(fontSize).FontColor(color);
                            r.RelativeItem().Text(t => WalkRich(t, li, fontSize, color, bold, italic, underline: false, strike: false, highlight: null));
                        });
                    }
                });
                break;
            }
            case "hr":
                col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                break;
            case "table":
                RenderRichTable(col, node, fontSize, color);
                break;
            default:
                // Unknown wrapper — recurse so nested <p>/<table> still render.
                foreach (var child in node.ChildNodes) RenderBlock(col, child, fontSize, color, bold, italic);
                break;
        }
    }

    /// <summary>Renders a TipTap-emitted &lt;table&gt; with rich inline content per cell.</summary>
    private static void RenderRichTable(QuestPDF.Fluent.ColumnDescriptor col, HtmlAgilityPack.HtmlNode tableNode,
        float fontSize, string color)
    {
        var rows = tableNode.SelectNodes(".//tr");
        if (rows is null || rows.Count == 0) return;
        var firstRowCells = rows[0].SelectNodes("./th|./td")?.Count ?? 0;
        if (firstRowCells == 0) return;

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < firstRowCells; i++)
                    cols.RelativeColumn();
            });

            foreach (var tr in rows)
            {
                var cells = tr.SelectNodes("./th|./td");
                if (cells is null) continue;
                foreach (var cell in cells)
                {
                    var isHeader = cell.Name.Equals("th", StringComparison.OrdinalIgnoreCase);
                    var box = table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3);
                    if (isHeader) box = box.Background(Colors.Grey.Lighten4);
                    box.Text(t => WalkRich(t, cell, fontSize, color, bold: isHeader, italic: false, underline: false, strike: false, highlight: null));
                }
            }
        });
    }

    private static (string Align, string Color) ParseBlockStyle(HtmlAgilityPack.HtmlNode node, string fallbackColor)
    {
        var styleAttr = node.GetAttributeValue("style", string.Empty);
        var c = fallbackColor;
        var a = string.Empty;
        if (!string.IsNullOrEmpty(styleAttr))
        {
            var m1 = System.Text.RegularExpressions.Regex.Match(styleAttr, @"color\s*:\s*([^;]+)");
            if (m1.Success) c = m1.Groups[1].Value.Trim();
            var m2 = System.Text.RegularExpressions.Regex.Match(styleAttr, @"text-align\s*:\s*([^;]+)");
            if (m2.Success) a = m2.Groups[1].Value.Trim().ToLowerInvariant();
        }
        return (a, c);
    }

    private static void WalkRich(QuestPDF.Fluent.TextDescriptor t, HtmlAgilityPack.HtmlNode node,
        float fontSize, string color, bool bold, bool italic, bool underline, bool strike, string? highlight)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlAgilityPack.HtmlNodeType.Text)
            {
                var text = System.Net.WebUtility.HtmlDecode(child.InnerText);
                if (string.IsNullOrEmpty(text)) continue;
                var span = t.Span(text).FontSize(fontSize).FontColor(color);
                if (bold) span.Bold();
                if (italic) span.Italic();
                if (underline) span.Underline();
                if (strike) span.Strikethrough();
                if (!string.IsNullOrEmpty(highlight)) span.BackgroundColor(highlight);
                continue;
            }
            if (child.NodeType != HtmlAgilityPack.HtmlNodeType.Element) continue;

            var name = child.Name.ToLowerInvariant();
            if (name == "br") { t.Line(""); continue; }

            var nb = bold; var ni = italic; var nu = underline; var ns = strike;
            var nfs = fontSize; var nc = color; var nh = highlight;
            var styleAttr = child.GetAttributeValue("style", string.Empty);
            if (!string.IsNullOrEmpty(styleAttr))
            {
                var m1 = System.Text.RegularExpressions.Regex.Match(styleAttr, @"(?<![-\w])color\s*:\s*([^;]+)");
                if (m1.Success) nc = m1.Groups[1].Value.Trim();
                var m2 = System.Text.RegularExpressions.Regex.Match(styleAttr, @"background-color\s*:\s*([^;]+)");
                if (m2.Success) nh = m2.Groups[1].Value.Trim();
            }
            if (name == "strong" || name == "b") nb = true;
            else if (name == "em" || name == "i") ni = true;
            else if (name == "u") nu = true;
            else if (name == "s" || name == "strike" || name == "del") ns = true;
            else if (name == "mark") nh = string.IsNullOrEmpty(nh) ? "#fff59d" : nh;
            else if (name == "h1") { nb = true; nfs = fontSize * 1.6f; }
            else if (name == "h2") { nb = true; nfs = fontSize * 1.35f; }
            else if (name == "h3") { nb = true; nfs = fontSize * 1.15f; }

            WalkRich(t, child, nfs, nc, nb, ni, nu, ns, nh);
            if (name == "p" || name == "div" || name == "li" ||
                name == "h1" || name == "h2" || name == "h3")
                t.Line("");
        }
    }

    private static (string EctsGrade, string UkGrade, double GradePoint) MapScore(decimal score)
    {
        var s = (int)Math.Floor(score);
        if (s >= 75) return ("A",  "A+", 5.0);
        if (s >= 70) return ("A",  "A",  5.0);
        if (s >= 65) return ("B",  "A-", 4.0);
        if (s >= 60) return ("C",  "B+", 3.0);
        if (s >= 55) return ("C",  "B",  3.0);
        if (s >= 50) return ("D",  "B-", 2.0);
        if (s >= 45) return ("D",  "C+", 2.0);
        if (s >= 41) return ("E",  "C",  1.0);
        if (s == 40) return ("E",  "C-", 1.0);
        if (s >= 30) return ("FX", "F",  0.0);
        return            ("F",  "F",  0.0);
    }

    private static readonly (string IbssGrade, string UkGrade, string UsGrade, string EctsGrade, string Points, string Remark)[] GradeStandardRows =
    {
        ("75-100", "75-100", "A+", "A",  "5.0", "Excellent – outstanding performance with only minor errors"),
        ("70-74",  "70-74",  "A",  "A",  "5.0", "Excellent – outstanding performance with only minor errors"),
        ("65-69",  "65-69",  "A-", "B",  "4.0", "Very good – above the average standard but with some errors"),
        ("60-64",  "60-64",  "B+", "C",  "3.0", "Good – generally sound work with a number of notable errors"),
        ("55-59",  "55-59",  "B",  "C",  "3.0", "Good – generally sound work with a number of notable errors"),
        ("50-54",  "50-54",  "B-", "D",  "2.0", "Satisfactory – fair but with significant shortcomings"),
        ("45-49",  "45-49",  "C+", "D",  "2.0", "Satisfactory – fair but with significant shortcomings"),
        ("41-44",  "41-44",  "C",  "E",  "1.0", "Sufficient – performance meets the minimum criteria"),
        ("40",     "40",     "C-", "E",  "1.0", "Sufficient – performance meets the minimum criteria"),
        ("30-39",  "30-39",  "F",  "FX", "0.0", "Fail – some more work required such as retaking exam before the credit can be awarded"),
        ("0-29",   "0-29",   "F",  "F",  "0.0", "Fail - retake credits"),
    };

    private static void RenderGradeStandardTable(
        QuestPDF.Fluent.LayersDescriptor layers,
        CertificateField field,
        float sx, float sy,
        string? schoolName = null)
    {
        var x = (float)field.X * sx;
        var y = (float)field.Y * sy;
        var w = Math.Max(50f, (float)(field.Width <= 0 ? 800 : field.Width) * sx);
        var fontSize = Math.Max(6f, field.FontSize <= 0 ? 9f : field.FontSize * sy);

        layers.Layer().PaddingTop(y).PaddingLeft(x).Width(w).Element(c =>
        {
            c.Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(4);
                });
                // First column is the school's own grade scale; label it with the
                // awarding school (IBAS/IBSS/…), or the neutral "School" fallback.
                var gradeCol = string.IsNullOrWhiteSpace(schoolName) ? "School Grade" : $"{schoolName.Trim()} Grade";
                Header(table, gradeCol);
                Header(table, "UK Grade");
                Header(table, "US Grade");
                Header(table, "ECTS Grade");
                Header(table, "ECTS Grade Points");
                Header(table, "Remark");
                foreach (var r in GradeStandardRows)
                {
                    Cell(table, r.IbssGrade, fontSize);
                    Cell(table, r.UkGrade,   fontSize);
                    Cell(table, r.UsGrade,   fontSize);
                    Cell(table, r.EctsGrade, fontSize);
                    Cell(table, r.Points,    fontSize);
                    Cell(table, r.Remark,    fontSize, alignLeft: true);
                }
            });
        });

        static void Header(QuestPDF.Fluent.TableDescriptor t, string text)
        {
            t.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).AlignCenter().AlignMiddle().Text(text).Bold().FontSize(9);
        }
        static void Cell(QuestPDF.Fluent.TableDescriptor t, string text, float fontSize, bool alignLeft = false)
        {
            var cell = t.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(3).MinHeight(20);
            var aligned = alignLeft ? cell.AlignLeft() : cell.AlignCenter();
            aligned.AlignMiddle().Text(text).FontSize(fontSize);
        }
    }

    private static QuestPDF.Helpers.PageSize ResolvePageSize(CertificateLayout layout, int lw, int lh)
    {
        var name = (layout.PageSize ?? "A4").Trim().ToLowerInvariant();
        var orient = (layout.Orientation ?? "auto").Trim().ToLowerInvariant();
        var landscape = orient switch
        {
            "landscape" => true,
            "portrait"  => false,
            _ => lw >= lh,
        };
        QuestPDF.Helpers.PageSize portrait = name switch
        {
            "a3"     => PageSizes.A3,
            "letter" => PageSizes.Letter,
            "custom" => new QuestPDF.Helpers.PageSize(lw, lh),
            _        => PageSizes.A4,
        };
        return landscape ? portrait.Landscape() : portrait;
    }

    /// <summary>
    /// Returns asset ids referenced by the certificate layout — backgrounds
    /// (per page) plus image-kind fields. The release service uses this to
    /// pre-fetch bytes before invoking the (synchronous) renderer.
    /// </summary>
    public static IReadOnlyList<Guid> ExtractCertificateAssetIds(CertificateLayout layout)
    {
        var ids = new HashSet<Guid>();
        foreach (var p in layout.GetPages())
        {
            if (p.BackgroundAssetId is { } bg) ids.Add(bg);
            foreach (var f in p.Fields ?? new())
            {
                var kind = (f.Kind ?? "text").Trim().ToLowerInvariant();
                if (kind == "image" && f.ImageAssetId is { } id) ids.Add(id);
            }
        }
        return ids.ToList();
    }

    private static string ResolveFieldText(CertificateField field, IReadOnlyDictionary<string, string> tagValues)
    {
        var body = field.Text ?? string.Empty;
        if (!string.IsNullOrEmpty(field.Tag) && tagValues.TryGetValue(field.Tag, out var v))
            body = v ?? string.Empty;
        // For tag fields with empty values we still emit prefix/suffix only if
        // body is non-empty — letting an unset tag suppress the whole field
        // (avoids a stray "Student ID: " when the student has no number).
        if (string.IsNullOrEmpty(body)) return string.Empty;
        return $"{field.Prefix ?? string.Empty}{body}{field.Suffix ?? string.Empty}";
    }

    public byte[] RenderHtml(
        string bodyHtml,
        IReadOnlyDictionary<string, string> tagValues,
        IReadOnlyDictionary<Guid, byte[]>? assetBytes = null)
        => RenderHtml(new[] { bodyHtml ?? string.Empty }, tagValues, assetBytes);

    public byte[] RenderHtml(
        IReadOnlyList<string> bodyHtmlPages,
        IReadOnlyDictionary<string, string> tagValues,
        IReadOnlyDictionary<Guid, byte[]>? assetBytes = null)
    {
        var assets = assetBytes ?? new Dictionary<Guid, byte[]>();
        var pages = (bodyHtmlPages ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        if (pages.Count == 0) pages.Add(string.Empty);

        return Document.Create(container =>
        {
            foreach (var raw in pages)
            {
                var substituted = SubstituteTags(raw ?? string.Empty, tagValues);
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml($"<root>{substituted}</root>");
                var root = doc.DocumentNode.SelectSingleNode("//root") ?? doc.DocumentNode;

                container.Page(page =>
                {
                    page.Margin(2, Unit.Centimetre);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(t => t.FontSize(11).FontColor(Colors.Black));

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);
                        foreach (var node in root.ChildNodes)
                            RenderNode(col, node, assets);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    /// <summary>
    /// Returns the unique asset ids referenced as <c>&lt;img data-asset-id="..."&gt;</c>
    /// across one or more HTML page bodies. Used by the release pipeline to
    /// pre-fetch image bytes before invoking the (sync) QuestPDF renderer.
    /// </summary>
    public static IReadOnlyList<Guid> ExtractAssetIds(string? html)
        => ExtractAssetIds(new[] { html ?? string.Empty });

    public static IReadOnlyList<Guid> ExtractAssetIds(IEnumerable<string?> htmlPages)
    {
        var seen = new HashSet<Guid>();
        foreach (var html in htmlPages ?? Array.Empty<string>())
        {
            if (string.IsNullOrEmpty(html)) continue;
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var imgs = doc.DocumentNode.SelectNodes("//img[@data-asset-id]");
            if (imgs is null) continue;
            foreach (var img in imgs)
            {
                var raw = img.GetAttributeValue("data-asset-id", string.Empty);
                if (Guid.TryParse(raw, out var id)) seen.Add(id);
            }
        }
        return seen.ToList();
    }

    private static string SubstituteTags(string html, IReadOnlyDictionary<string, string> tagValues)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var working = html;
        foreach (var (token, value) in tagValues)
            working = working.Replace(token, value, StringComparison.OrdinalIgnoreCase);
        return working;
    }

    private static void RenderNode(ColumnDescriptor col, HtmlNode node, IReadOnlyDictionary<Guid, byte[]> assets)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            var text = System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();
            if (!string.IsNullOrEmpty(text))
                col.Item().Text(text);
            return;
        }

        if (node.NodeType != HtmlNodeType.Element) return;

        switch (node.Name.ToLowerInvariant())
        {
            case "h1":
                col.Item().Text(InlineText(node)).FontSize(20).Bold();
                break;
            case "h2":
                col.Item().Text(InlineText(node)).FontSize(16).Bold();
                break;
            case "h3":
                col.Item().Text(InlineText(node)).FontSize(14).Bold();
                break;
            case "p":
            case "div":
                {
                    // A paragraph that wraps a single image (Google Docs / TipTap
                    // pattern) needs to render the image, not just the InnerText.
                    var imgChild = node.SelectNodes(".//img[@data-asset-id]")?.FirstOrDefault();
                    if (imgChild is not null)
                    {
                        RenderImage(col, imgChild, assets);
                        var residual = InlineText(node);
                        if (!string.IsNullOrWhiteSpace(residual))
                            col.Item().Text(residual);
                        break;
                    }
                    var text = InlineText(node);
                    if (!string.IsNullOrWhiteSpace(text))
                        col.Item().Text(text);
                    break;
                }
            case "img":
                RenderImage(col, node, assets);
                break;
            case "ul":
                foreach (var li in node.SelectNodes("./li") ?? Enumerable.Empty<HtmlNode>())
                    col.Item().Text("•  " + InlineText(li));
                break;
            case "ol":
                {
                    var lis = node.SelectNodes("./li");
                    if (lis is null) break;
                    var i = 1;
                    foreach (var li in lis)
                        col.Item().Text($"{i++}.  {InlineText(li)}");
                    break;
                }
            case "br":
                col.Item().Text(string.Empty);
                break;
            case "hr":
                col.Item().LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
                break;
            case "table":
                RenderTable(col, node);
                break;
            default:
                {
                    // Recurse into unknown wrappers (span, section, article…)
                    foreach (var child in node.ChildNodes)
                        RenderNode(col, child, assets);
                    break;
                }
        }
    }

    private static void RenderImage(ColumnDescriptor col, HtmlNode imgNode, IReadOnlyDictionary<Guid, byte[]> assets)
    {
        var raw = imgNode.GetAttributeValue("data-asset-id", string.Empty);
        if (!Guid.TryParse(raw, out var id) || !assets.TryGetValue(id, out var bytes) || bytes is null || bytes.Length == 0)
            return;
        col.Item().Image(bytes).FitWidth();
    }

    private static string InlineText(HtmlNode node)
        => System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();

    private static void RenderTable(ColumnDescriptor col, HtmlNode tableNode)
    {
        var rows = tableNode.SelectNodes(".//tr");
        if (rows is null || rows.Count == 0) return;
        var firstRowCells = rows[0].SelectNodes("./th|./td")?.Count ?? 0;
        if (firstRowCells == 0) return;

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < firstRowCells; i++)
                    cols.RelativeColumn();
            });

            foreach (var tr in rows)
            {
                var cells = tr.SelectNodes("./th|./td");
                if (cells is null) continue;
                var isHeader = cells.Any(c => c.Name.Equals("th", StringComparison.OrdinalIgnoreCase));
                foreach (var cell in cells)
                {
                    var text = InlineText(cell);
                    var cellContainer = table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4);
                    if (isHeader)
                        cellContainer.Background(Colors.Grey.Lighten3).Text(text).Bold();
                    else
                        cellContainer.Text(text);
                }
            }
        });
    }
}

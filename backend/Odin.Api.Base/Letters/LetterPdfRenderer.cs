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
        IReadOnlyList<TranscriptGradeRow>? transcriptRows = null)
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

        return Document.Create(container =>
        {
            foreach (var pageDef in layout.GetPages())
            {
                container.Page(page =>
                {
                    page.Size(pageSize);
                    page.Margin(0);
                    page.PageColor(Colors.White);

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

                        foreach (var field in pageDef.Fields ?? new())
                        {
                            var kind = (field.Kind ?? "text").Trim().ToLowerInvariant();
                            if (kind == "transcripttable")
                            {
                                RenderTranscriptTable(layers, field, sx, sy, transcriptRows ?? Array.Empty<TranscriptGradeRow>());
                                continue;
                            }
                            if (kind == "gradestandardtable")
                            {
                                RenderGradeStandardTable(layers, field, sx, sy);
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

    private static void RenderTranscriptTable(
        QuestPDF.Fluent.LayersDescriptor layers,
        CertificateField field,
        float sx, float sy,
        IReadOnlyList<TranscriptGradeRow> rows)
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
                    cols.RelativeColumn(2);  // Code
                    cols.RelativeColumn(4);  // Module
                    cols.RelativeColumn(2);  // ECTS credit hours
                    cols.RelativeColumn(2);  // ECTS Grade
                    cols.RelativeColumn(2);  // IBSS Grade
                    cols.RelativeColumn(2);  // ECTS Grade Point
                    cols.RelativeColumn(2);  // Grade Point
                });

                Header(table, "Code");
                Header(table, "Module");
                Header(table, "ECTS\ncredit hours");
                Header(table, "ECTS\nGrade");
                Header(table, "IBSS\nGrade");
                Header(table, "ECTS\nGrade Point");
                Header(table, "Grade\nPoint");

                decimal totalEcts = 0m;
                double totalGp = 0;
                foreach (var row in rows)
                {
                    var (ects, uk, gp) = MapScore(row.Score);
                    var pts = (double)row.Ects * gp;
                    totalEcts += row.Ects;
                    totalGp += pts;
                    Cell(table, row.Code,                     fontSize);
                    Cell(table, row.Name,                     fontSize, alignLeft: true);
                    Cell(table, row.Ects.ToString("0.##", CultureInfo.InvariantCulture), fontSize);
                    Cell(table, ects,                         fontSize);
                    Cell(table, uk,                           fontSize);
                    Cell(table, gp.ToString("0.0", CultureInfo.InvariantCulture), fontSize);
                    Cell(table, pts.ToString("0.0", CultureInfo.InvariantCulture), fontSize);
                }

                // Pad with blank rows so the table looks like the reference (10 rows).
                var minRows = 10;
                for (int i = rows.Count; i < minRows; i++)
                {
                    for (int j = 0; j < 7; j++) Cell(table, "", fontSize);
                }

                // Total row
                table.Cell().ColumnSpan(2).Border(0.5f).BorderColor(Colors.Black).Padding(3).Text("Total").Bold().FontSize(fontSize);
                Cell(table, totalEcts.ToString("0.##", CultureInfo.InvariantCulture), fontSize, bold: true);
                Cell(table, "", fontSize);
                Cell(table, "", fontSize);
                Cell(table, "", fontSize);
                Cell(table, totalGp.ToString("0.0", CultureInfo.InvariantCulture), fontSize, bold: true);

                // GPA row
                var gpa = totalEcts > 0 ? totalGp / (double)totalEcts : 0;
                table.Cell().ColumnSpan(6).Border(0.5f).BorderColor(Colors.Black).Padding(3).AlignRight()
                    .Text("Grade Point Average").Bold().FontSize(fontSize);
                Cell(table, gpa.ToString("0.00", CultureInfo.InvariantCulture), fontSize, bold: true);
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
        float sx, float sy)
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
                Header(table, "IBSS Grade");
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

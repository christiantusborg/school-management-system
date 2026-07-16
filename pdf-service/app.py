"""
Generic PDF field-extraction + form-fill service.

Sits next to the .NET WebApi as the "firm-local Python service" the
ADR-0039 AcroFormFill strategy mentions. Two responsibilities:

1. POST /extract-fields    -> read every AcroForm widget (including XFA
                               variants like the USCIS i-130) and return
                               the list of field names + types. The SPA
                               falls back to this when its in-browser
                               pdf-lib parse returns zero fields.

2. POST /fill              -> apply a mapping of field-name -> value to
                               an uploaded PDF, optionally flatten, and
                               return the filled PDF bytes. The .NET side
                               will call this at "generate intake output"
                               time (not wired yet).

Adapted from ~/projects/SysCalender/pdf-backend/app.py but rewritten so
the field discovery is GENERIC (not hard-coded to one form).
"""
import io
import logging
import os
from flask import Flask, request, jsonify, send_file
from flask_cors import CORS
import fitz  # PyMuPDF


logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Flask(__name__)
CORS(app)

# Allow upload up to 25 MB. Templates are config, not client data; the
# practical ceiling is "the largest government form anyone files".
app.config["MAX_CONTENT_LENGTH"] = 25 * 1024 * 1024


@app.route("/health", methods=["GET"])
def health():
    return jsonify({"status": "healthy", "service": "QuVian PDF service", "engine": "PyMuPDF"})


@app.route("/extract-fields", methods=["POST"])
def extract_fields():
    """Read every AcroForm field from the uploaded PDF.

    Accepts the file via either multipart upload (field name `file`) or
    JSON body `{ "bytesBase64": "..." }` so the SPA can call it directly
    after building a base64 string from a `<input type=file>`.
    """
    pdf_bytes = _read_pdf_bytes(request)
    if pdf_bytes is None:
        return jsonify({"error": "no PDF supplied (use multipart `file` or JSON `bytesBase64`)"}), 400

    try:
        doc = fitz.open(stream=pdf_bytes, filetype="pdf")
    except Exception as exc:
        logger.exception("PyMuPDF could not open the uploaded PDF")
        return jsonify({"error": f"failed to open PDF: {exc}"}), 400

    fields = []
    try:
        for page_index, page in enumerate(doc, start=1):
            for widget in page.widgets() or []:
                fields.append({
                    "name": widget.field_name,
                    "type": _field_type_label(widget.field_type),
                    "page": page_index,
                    "rect": [round(widget.rect.x0, 2), round(widget.rect.y0, 2),
                             round(widget.rect.x1, 2), round(widget.rect.y1, 2)],
                    "choices": _flatten_choices(widget.choice_values),
                    "isReadonly": bool(widget.field_flags & fitz.PDF_FIELD_IS_READ_ONLY),
                })
    finally:
        doc.close()

    # De-duplicate by name while preserving order (multi-page widgets
    # repeat the same field name; the SPA only needs one row per).
    seen = set()
    unique = []
    for f in fields:
        if f["name"] in seen:
            continue
        seen.add(f["name"])
        unique.append(f)

    return jsonify({"count": len(unique), "fields": unique})


@app.route("/fill", methods=["POST"])
def fill():
    """Fill an AcroForm PDF.

    Expects JSON: `{ "bytesBase64": "...", "values": {"field_name": "value", ...},
                     "flatten": false, "filename": "filled.pdf" }`
    Returns the filled PDF bytes (Content-Type: application/pdf).
    """
    body = request.get_json(silent=True) or {}
    pdf_bytes = _read_pdf_bytes(request, body)
    values = body.get("values") or {}
    flatten = bool(body.get("flatten", False))
    filename = body.get("filename") or "filled.pdf"

    if pdf_bytes is None:
        return jsonify({"error": "no PDF supplied"}), 400
    if not isinstance(values, dict):
        return jsonify({"error": "`values` must be an object"}), 400

    try:
        doc = fitz.open(stream=pdf_bytes, filetype="pdf")
    except Exception as exc:
        return jsonify({"error": f"failed to open PDF: {exc}"}), 400

    try:
        filled, missing = 0, []
        for page in doc:
            for widget in page.widgets() or []:
                if widget.field_name in values:
                    widget.field_value = str(values[widget.field_name])
                    widget.update()
                    filled += 1
                else:
                    missing.append(widget.field_name)

        if flatten:
            # Render every page to an image, replace the page contents
            # with the image, and drop widget annotations. Loses
            # interactivity but the result prints/archives identically
            # across viewers.
            for page in doc:
                pix = page.get_pixmap(dpi=200)
                page.clean_contents()
                page.insert_image(page.rect, pixmap=pix, overlay=True)

        out = io.BytesIO()
        doc.save(out, deflate=True)
        out.seek(0)
    finally:
        doc.close()

    logger.info("filled %d field(s); %d unmapped", filled, len(missing))
    return send_file(out, mimetype="application/pdf",
                     as_attachment=True, download_name=filename)


# ── helpers ────────────────────────────────────────────────────────────

def _read_pdf_bytes(req, json_body=None):
    """Pull PDF bytes from either multipart or JSON body."""
    if "file" in req.files:
        return req.files["file"].read()
    body = json_body if json_body is not None else (req.get_json(silent=True) or {})
    b64 = body.get("bytesBase64")
    if not b64:
        return None
    import base64
    try:
        return base64.b64decode(b64)
    except Exception:
        return None


_FIELD_TYPE_LABELS = {
    fitz.PDF_WIDGET_TYPE_TEXT: "text",
    fitz.PDF_WIDGET_TYPE_CHECKBOX: "checkbox",
    fitz.PDF_WIDGET_TYPE_RADIOBUTTON: "radio",
    fitz.PDF_WIDGET_TYPE_COMBOBOX: "combo",
    fitz.PDF_WIDGET_TYPE_LISTBOX: "listbox",
    fitz.PDF_WIDGET_TYPE_SIGNATURE: "signature",
    fitz.PDF_WIDGET_TYPE_BUTTON: "button",
}


def _field_type_label(t):
    return _FIELD_TYPE_LABELS.get(t, f"unknown({t})")


def _flatten_choices(choices):
    """PyMuPDF returns combo/list options as either a flat tuple of
    strings or a tuple of (value, label) pairs depending on the widget.
    Normalize to a flat list[str] so JSON consumers don't deal with the
    polymorphism."""
    if not choices:
        return None
    out = []
    for c in choices:
        if isinstance(c, (tuple, list)) and len(c) > 0:
            out.append(str(c[-1]))
        else:
            out.append(str(c))
    return out


if __name__ == "__main__":
    # Default 5002 to match the .NET WebApi's PdfService:BaseUrl fallback
    # (http://127.0.0.1:5002). Override with PDF_SERVICE_PORT if needed.
    port = int(os.environ.get("PDF_SERVICE_PORT", "5002"))
    # 0.0.0.0 so vite's dev proxy can reach it from the SPA process.
    # In production this binds to localhost only and the .NET WebApi
    # speaks to it directly over loopback.
    host = os.environ.get("PDF_SERVICE_HOST", "127.0.0.1")
    app.run(host=host, port=port, debug=bool(os.environ.get("PDF_SERVICE_DEBUG")))

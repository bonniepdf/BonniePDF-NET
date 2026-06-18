import org.verapdf.gf.foundry.VeraGreenfieldFoundryProvider;
import org.verapdf.pdfa.Foundries;
import org.verapdf.pdfa.PDFAParser;
import org.verapdf.pdfa.PDFAValidator;
import org.verapdf.pdfa.flavours.PDFAFlavour;
import org.verapdf.pdfa.results.TestAssertion;
import org.verapdf.pdfa.results.ValidationResult;

import java.io.File;
import java.io.FileInputStream;
import java.io.FileWriter;
import java.io.IOException;
import java.io.Writer;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Self-contained veraPDF compliance gate.
 *
 * Usage: Validate &lt;pdf-or-dir&gt; &lt;flavour[,flavour...]&gt;
 *   e.g. Validate generated-pdfs ua1,1b
 *        Validate generated-pdfs auto
 *
 * Validates every PDF (a single file, or every *.pdf in a directory)
 * against every requested flavour. When the flavour list is `auto`, the
 * validator derives the veraPDF profiles from each file's XMP conformance
 * claims (`pdfaid` and `pdfuaid`) instead of forcing one fixed profile across
 * the whole directory. Prints a per-file/flavour verdict and
 * the failing clause ids. Exits 0 only if EVERY file is compliant with
 * EVERY flavour; exits 1 otherwise (so it gates CI).
 *
 * Outputs, all best-effort (a write failure never changes the exit code):
 *   - a Markdown summary table to $GITHUB_STEP_SUMMARY (if set) and to
 *     ./verapdf-summary.md, so the CI job summary can show results;
 *   - a self-contained HTML card report (verapdf-report.html) written into the
 *     PDF directory, one card per file with its first-page SVG preview, a pill
 *     per claimed conformance level, and the file size. Each card references its
 *     sibling `<name>.svg` via &lt;img&gt; (the previews are emitted next to the
 *     PDFs during generation), which isolates each SVG and keeps the report tiny.
 */
public final class Validate {

    /** One flavour verdict for a single file. */
    private static final class Verdict {
        final String flavour;
        final String status; // PASS | FAIL | ERROR
        final String details;

        Verdict(String flavour, String status, String details) {
            this.flavour = flavour;
            this.status = status;
            this.details = details == null ? "" : details;
        }
    }

    /** All verdicts for a single file, plus its size and preview filename. */
    private static final class FileReport {
        final String name;
        final long sizeBytes;
        final String svgName; // sibling preview filename, or null if absent
        final List<Verdict> verdicts = new ArrayList<>();

        FileReport(String name, long sizeBytes, String svgName) {
            this.name = name;
            this.sizeBytes = sizeBytes;
            this.svgName = svgName;
        }

        boolean passed() {
            for (Verdict v : verdicts) {
                if (!"PASS".equals(v.status)) {
                    return false;
                }
            }
            return !verdicts.isEmpty();
        }
    }

    public static void main(String[] args) throws Exception {
        if (args.length < 2) {
            System.err.println("Usage: Validate <pdf-or-dir> <flavour[,flavour...]>");
            System.exit(2);
        }

        File target = new File(args[0]);
        List<File> pdfs = new ArrayList<>();
        if (target.isDirectory()) {
            File[] found = target.listFiles((d, n) -> n.toLowerCase().endsWith(".pdf"));
            if (found != null) {
                Arrays.sort(found);
                pdfs.addAll(Arrays.asList(found));
            }
        } else {
            pdfs.add(target);
        }
        if (pdfs.isEmpty()) {
            System.err.println("ERROR: no PDF files found at " + target);
            System.exit(2);
        }

        boolean autoFlavours = "auto".equalsIgnoreCase(args[1].trim());
        List<PDFAFlavour> requestedFlavours = new ArrayList<>();
        if (!autoFlavours) {
            for (String f : args[1].split(",")) {
                String trimmed = f.trim();
                if (!trimmed.isEmpty()) {
                    requestedFlavours.add(PDFAFlavour.fromString(trimmed));
                }
            }
        }

        VeraGreenfieldFoundryProvider.initialise();
        boolean allPass = true;
        List<FileReport> reports = new ArrayList<>();

        for (File pdf : pdfs) {
            FileReport report = new FileReport(pdf.getName(), pdf.length(), svgSibling(pdf));
            reports.add(report);

            List<PDFAFlavour> flavours = autoFlavours ? detectFlavours(pdf) : requestedFlavours;
            if (flavours.isEmpty()) {
                System.out.printf("FAIL  %-28s [auto] no PDF/A or PDF/UA XMP conformance claim found%n", pdf.getName());
                allPass = false;
                report.verdicts.add(new Verdict("auto", "FAIL", "No PDF/A or PDF/UA XMP conformance claim found"));
                continue;
            }

            for (PDFAFlavour flavour : flavours) {
                boolean compliant = false;
                Map<String, int[]> failures = new LinkedHashMap<>();
                Map<String, String> messages = new LinkedHashMap<>();
                try (PDFAParser parser = Foundries.defaultInstance()
                        .createParser(new FileInputStream(pdf), flavour)) {
                    PDFAValidator validator = Foundries.defaultInstance().createValidator(flavour, false);
                    ValidationResult result = validator.validate(parser);
                    compliant = result.isCompliant();
                    for (TestAssertion a : result.getTestAssertions()) {
                        if (a.getStatus() == TestAssertion.Status.FAILED) {
                            String clause = a.getRuleId().getClause() + "-" + a.getRuleId().getTestNumber();
                            failures.computeIfAbsent(clause, k -> new int[1])[0]++;
                            messages.putIfAbsent(clause, a.getMessage());
                        }
                    }
                } catch (Exception e) {
                    System.out.printf("ERROR %-28s [%s] %s%n", pdf.getName(), flavour, e.getMessage());
                    allPass = false;
                    report.verdicts.add(new Verdict(flavour.toString(), "ERROR", String.valueOf(e.getMessage())));
                    continue;
                }

                if (compliant) {
                    System.out.printf("PASS  %-28s [%s]%n", pdf.getName(), flavour);
                    report.verdicts.add(new Verdict(flavour.toString(), "PASS", ""));
                } else {
                    allPass = false;
                    System.out.printf("FAIL  %-28s [%s]%n", pdf.getName(), flavour);
                    StringBuilder details = new StringBuilder();
                    for (Map.Entry<String, int[]> e : failures.entrySet()) {
                        System.out.printf("        %s (x%d): %s%n",
                                e.getKey(), e.getValue()[0], messages.get(e.getKey()));
                        if (details.length() > 0) {
                            details.append("<br>");
                        }
                        details.append(e.getKey()).append(" (x").append(e.getValue()[0]).append("): ")
                                .append(messages.get(e.getKey()));
                    }
                    report.verdicts.add(new Verdict(flavour.toString(), "FAIL", details.toString()));
                }
            }
        }

        System.out.println(allPass ? "\nveraPDF gate: PASS" : "\nveraPDF gate: FAIL");
        writeSummary(reports, allPass);
        File reportDir = target.isDirectory() ? target : target.getAbsoluteFile().getParentFile();
        writeHtmlReport(reports, allPass, reportDir);
        System.exit(allPass ? 0 : 1);
    }


    private static List<PDFAFlavour> detectFlavours(File pdf) throws IOException {
        String text = Files.readString(pdf.toPath(), StandardCharsets.ISO_8859_1);
        List<PDFAFlavour> flavours = new ArrayList<>();

        String pdfAPart = firstXmlElementValue(text, "pdfaid", "part");
        if (pdfAPart != null) {
            String pdfAConformance = firstXmlElementValue(text, "pdfaid", "conformance");
            String flavour = pdfAFlavourName(pdfAPart, pdfAConformance);
            if (flavour != null) {
                flavours.add(PDFAFlavour.fromString(flavour));
            }
        }

        String pdfUaPart = firstXmlElementValue(text, "pdfuaid", "part");
        if ("1".equals(pdfUaPart)) {
            flavours.add(PDFAFlavour.fromString("ua1"));
        }

        return flavours;
    }

    private static String pdfAFlavourName(String part, String conformance) {
        switch (part) {
            case "1":
            case "2":
            case "3":
                if (conformance == null || conformance.isEmpty()) {
                    return null;
                }
                return part + conformance.toLowerCase();
            case "4":
                return "4";
            default:
                return null;
        }
    }

    private static String firstXmlElementValue(String text, String prefix, String localName) {
        Pattern pattern = Pattern.compile("<" + Pattern.quote(prefix) + ":" + Pattern.quote(localName)
                + "\\b[^>]*>(.*?)</" + Pattern.quote(prefix) + ":" + Pattern.quote(localName) + ">",
                Pattern.CASE_INSENSITIVE | Pattern.DOTALL);
        Matcher matcher = pattern.matcher(text);
        if (!matcher.find()) {
            return null;
        }
        return matcher.group(1).trim();
    }

    /** Returns the sibling preview filename (e.g. foo.svg) if it exists, else null. */
    private static String svgSibling(File pdf) {
        String svgName = pdf.getName().replaceAll("(?i)\\.pdf$", ".svg");
        File parent = pdf.getAbsoluteFile().getParentFile();
        File svg = parent == null ? new File(svgName) : new File(parent, svgName);
        return svg.isFile() ? svgName : null;
    }

    /**
     * Writes a Markdown results table to $GITHUB_STEP_SUMMARY (appended, if the
     * env var is set) and to ./verapdf-summary.md (overwritten), so the CI job
     * summary can show the same table without re-parsing stdout.
     */
    private static void writeSummary(List<FileReport> reports, boolean allPass) {
        StringBuilder md = new StringBuilder();
        md.append("## veraPDF gate: ").append(allPass ? "PASS ✅" : "FAIL ❌").append("\n\n");
        md.append("See the **verapdf-report.html** artifact for first-page previews.\n\n");
        md.append("| File | Size | Flavour | Result | Details |\n");
        md.append("| --- | --- | --- | --- | --- |\n");
        for (FileReport r : reports) {
            for (Verdict v : r.verdicts) {
                String icon;
                if ("PASS".equals(v.status)) {
                    icon = "PASS ✅";
                } else if ("SKIP".equals(v.status)) {
                    icon = "SKIP ⏭️";
                } else if ("ERROR".equals(v.status)) {
                    icon = "ERROR ⚠️";
                } else {
                    icon = "FAIL ❌";
                }
                String details = v.details.isEmpty() ? "" : v.details.replace("|", "\\|");
                md.append("| ").append(r.name).append(" | ").append(humanSize(r.sizeBytes)).append(" | ")
                        .append(prettyFlavour(v.flavour)).append(" | ")
                        .append(icon).append(" | ").append(details).append(" |\n");
            }
        }

        String stepSummaryPath = System.getenv("GITHUB_STEP_SUMMARY");
        if (stepSummaryPath != null && !stepSummaryPath.isEmpty()) {
            try (Writer w = new FileWriter(stepSummaryPath, StandardCharsets.UTF_8, true)) {
                w.write("\n");
                w.write(md.toString());
            } catch (IOException e) {
                System.err.println("WARN: failed to write GITHUB_STEP_SUMMARY: " + e.getMessage());
            }
        }

        try (Writer w = new FileWriter("verapdf-summary.md", StandardCharsets.UTF_8)) {
            w.write(md.toString());
        } catch (IOException e) {
            System.err.println("WARN: failed to write verapdf-summary.md: " + e.getMessage());
        }
    }

    /**
     * Writes a self-contained HTML card gallery into {@code reportDir} (next to
     * the PDFs and their sibling SVG previews). One card per file: the first-page
     * preview (referenced as {@code <img src="name.svg">} so each SVG stays
     * isolated), a pill per claimed conformance level coloured by verdict, the
     * file size, and a collapsible failure list when something failed.
     */
    private static void writeHtmlReport(List<FileReport> reports, boolean allPass, File reportDir) {
        StringBuilder h = new StringBuilder();
        h.append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n");
        h.append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        h.append("<title>veraPDF conformance report</title>\n<style>\n").append(CSS).append("</style>\n");
        h.append("</head>\n<body>\n");

        h.append("<header class=\"summary ").append(allPass ? "ok" : "bad").append("\">");
        h.append("<h1>veraPDF gate: ").append(allPass ? "PASS ✅" : "FAIL ❌").append("</h1>");
        h.append("<p>").append(reports.size()).append(reports.size() == 1 ? " file validated" : " files validated").append("</p>");
        h.append("</header>\n");

        h.append("<main class=\"grid\">\n");
        for (FileReport r : reports) {
            h.append("<article class=\"card ").append(r.passed() ? "ok" : "bad").append("\">\n");

            h.append("<div class=\"thumb\">");
            if (r.svgName != null) {
                h.append("<img loading=\"lazy\" alt=\"First page of ").append(htmlEscape(r.name))
                        .append("\" src=\"").append(htmlEscape(r.svgName)).append("\">");
            } else {
                h.append("<div class=\"noprev\">no preview</div>");
            }
            h.append("</div>\n");

            h.append("<div class=\"meta\">\n");
            h.append("<h2 title=\"").append(htmlEscape(r.name)).append("\">").append(htmlEscape(r.name)).append("</h2>\n");

            h.append("<div class=\"pills\">");
            for (Verdict v : r.verdicts) {
                String cls = "PASS".equals(v.status) ? "pill ok"
                        : ("ERROR".equals(v.status) ? "pill warn" : "pill bad");
                h.append("<span class=\"").append(cls).append("\">")
                        .append(htmlEscape(prettyFlavour(v.flavour))).append("</span>");
            }
            h.append("<span class=\"pill size\">").append(humanSize(r.sizeBytes)).append("</span>");
            h.append("</div>\n");

            boolean anyDetail = false;
            for (Verdict v : r.verdicts) {
                if (!"PASS".equals(v.status) && !v.details.isEmpty()) {
                    anyDetail = true;
                    break;
                }
            }
            if (anyDetail) {
                h.append("<details><summary>Failures</summary><ul>");
                for (Verdict v : r.verdicts) {
                    if (!"PASS".equals(v.status) && !v.details.isEmpty()) {
                        h.append("<li><strong>").append(htmlEscape(prettyFlavour(v.flavour))).append(":</strong> ")
                                .append(htmlEscape(v.details.replace("<br>", "; "))).append("</li>");
                    }
                }
                h.append("</ul></details>\n");
            }

            h.append("</div>\n</article>\n");
        }
        h.append("</main>\n</body>\n</html>\n");

        File out = reportDir == null ? new File("verapdf-report.html") : new File(reportDir, "verapdf-report.html");
        try (Writer w = new FileWriter(out, StandardCharsets.UTF_8)) {
            w.write(h.toString());
        } catch (IOException e) {
            System.err.println("WARN: failed to write verapdf-report.html: " + e.getMessage());
        }
    }

    /** Normalises a veraPDF flavour token to a human label, e.g. "2b" -> "PDF/A-2B", "ua1" -> "PDF/UA-1". */
    private static String prettyFlavour(String flavour) {
        if (flavour == null || flavour.isEmpty()) {
            return "?";
        }
        if ("auto".equalsIgnoreCase(flavour)) {
            return "Unclaimed";
        }
        String s = flavour.toLowerCase(Locale.ROOT).replace("_", "");
        if (s.startsWith("pdfua")) {
            s = "ua" + s.substring(5);
        } else if (s.startsWith("pdfa")) {
            s = s.substring(4);
        }
        if (s.startsWith("ua")) {
            return "PDF/UA-" + s.substring(2);
        }
        if ("4".equals(s)) {
            return "PDF/A-4";
        }
        if (s.length() >= 2 && Character.isDigit(s.charAt(0))) {
            return "PDF/A-" + s.charAt(0) + s.substring(1).toUpperCase(Locale.ROOT);
        }
        return flavour;
    }

    private static String humanSize(long bytes) {
        if (bytes < 1024) {
            return bytes + " B";
        }
        double kb = bytes / 1024.0;
        if (kb < 1024) {
            return String.format(Locale.ROOT, "%.1f KB", kb);
        }
        return String.format(Locale.ROOT, "%.2f MB", kb / 1024.0);
    }

    private static String htmlEscape(String s) {
        if (s == null) {
            return "";
        }
        return s.replace("&", "&amp;")
                .replace("<", "&lt;")
                .replace(">", "&gt;")
                .replace("\"", "&quot;");
    }

    private static final String CSS =
            ":root{--bg:#0f172a;--fg:#e2e8f0;--card:#fff;--ink:#0f172a;--muted:#64748b;"
            + "--ok:#16a34a;--okbg:#dcfce7;--bad:#dc2626;--badbg:#fee2e2;--warn:#d97706;--warnbg:#fef3c7;--line:#e2e8f0}"
            + "*{box-sizing:border-box}"
            + "body{margin:0;font:15px/1.5 system-ui,-apple-system,Segoe UI,Roboto,sans-serif;color:var(--ink);background:#f8fafc}"
            + ".summary{padding:24px 32px;color:var(--fg);background:var(--bg)}"
            + ".summary h1{margin:0 0 4px;font-size:20px}"
            + ".summary p{margin:0;color:#94a3b8}"
            + ".summary.ok h1{color:#4ade80}.summary.bad h1{color:#f87171}"
            + ".grid{display:grid;gap:20px;grid-template-columns:repeat(auto-fill,minmax(260px,1fr));padding:24px 32px}"
            + ".card{background:var(--card);border:1px solid var(--line);border-radius:12px;overflow:hidden;"
            + "box-shadow:0 1px 2px rgba(15,23,42,.06);border-top:4px solid var(--muted)}"
            + ".card.ok{border-top-color:var(--ok)}.card.bad{border-top-color:var(--bad)}"
            + ".thumb{height:300px;display:flex;align-items:center;justify-content:center;"
            + "background:repeating-conic-gradient(#f1f5f9 0 25%,#fff 0 50%) 50%/24px 24px;padding:14px}"
            + ".thumb img{max-width:100%;max-height:100%;object-fit:contain;"
            + "box-shadow:0 2px 10px rgba(15,23,42,.18);background:#fff}"
            + ".thumb .noprev{color:var(--muted);font-size:13px;font-style:italic}"
            + ".meta{padding:14px 16px}"
            + ".meta h2{margin:0 0 10px;font-size:14px;font-weight:600;word-break:break-all}"
            + ".pills{display:flex;flex-wrap:wrap;gap:6px}"
            + ".pill{font-size:12px;font-weight:600;padding:3px 9px;border-radius:999px;white-space:nowrap}"
            + ".pill.ok{color:var(--ok);background:var(--okbg)}"
            + ".pill.bad{color:var(--bad);background:var(--badbg)}"
            + ".pill.warn{color:var(--warn);background:var(--warnbg)}"
            + ".pill.size{color:var(--muted);background:#f1f5f9}"
            + "details{margin-top:10px;font-size:12px;color:#334155}"
            + "summary{cursor:pointer;color:var(--bad);font-weight:600}"
            + "details ul{margin:8px 0 0;padding-left:18px}"
            + "details li{margin:4px 0;word-break:break-word}";
}

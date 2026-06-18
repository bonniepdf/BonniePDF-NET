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
 * Also writes a Markdown summary table to $GITHUB_STEP_SUMMARY (if set)
 * and to ./verapdf-summary.md, so CI can surface results in the run
 * summary without re-parsing stdout.
 */
public final class Validate {
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
        List<String[]> summaryRows = new ArrayList<>();

        for (File pdf : pdfs) {
            List<PDFAFlavour> flavours = autoFlavours ? detectFlavours(pdf) : requestedFlavours;
            if (flavours.isEmpty()) {
                System.out.printf("FAIL  %-28s [auto] no PDF/A or PDF/UA XMP conformance claim found%n", pdf.getName());
                allPass = false;
                summaryRows.add(new String[] { pdf.getName(), "auto", "FAIL", "No PDF/A or PDF/UA XMP conformance claim found" });
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
                    summaryRows.add(new String[] { pdf.getName(), flavour.toString(), "ERROR", String.valueOf(e.getMessage()) });
                    continue;
                }

                if (compliant) {
                    System.out.printf("PASS  %-28s [%s]%n", pdf.getName(), flavour);
                    summaryRows.add(new String[] { pdf.getName(), flavour.toString(), "PASS", "" });
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
                    summaryRows.add(new String[] { pdf.getName(), flavour.toString(), "FAIL", details.toString() });
                }
            }
        }

        System.out.println(allPass ? "\nveraPDF gate: PASS" : "\nveraPDF gate: FAIL");
        writeSummary(summaryRows, allPass);
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

    /**
     * Writes a Markdown results table to $GITHUB_STEP_SUMMARY (appended, if
     * the env var is set) and to ./verapdf-summary.md (overwritten), so the
     * CI job summary can show results without re-parsing stdout.
     */
    private static void writeSummary(List<String[]> rows, boolean allPass) {
        StringBuilder md = new StringBuilder();
        md.append("## veraPDF gate: ").append(allPass ? "PASS ✅" : "FAIL ❌").append("\n\n");
        md.append("| File | Flavour | Result | Details |\n");
        md.append("| --- | --- | --- | --- |\n");
        for (String[] row : rows) {
            String icon;
            if ("PASS".equals(row[2])) {
                icon = "PASS ✅";
            } else if ("SKIP".equals(row[2])) {
                icon = "SKIP ⏭️";
            } else if ("ERROR".equals(row[2])) {
                icon = "ERROR ⚠️";
            } else {
                icon = "FAIL ❌";
            }
            String details = row[3].isEmpty() ? "" : row[3].replace("|", "\\|");
            md.append("| ").append(row[0]).append(" | ").append(row[1]).append(" | ")
                    .append(icon).append(" | ").append(details).append(" |\n");
        }

        String stepSummaryPath = System.getenv("GITHUB_STEP_SUMMARY");
        if (stepSummaryPath != null && !stepSummaryPath.isEmpty()) {
            try (Writer w = new FileWriter(stepSummaryPath, true)) {
                w.write("\n");
                w.write(md.toString());
            } catch (IOException e) {
                System.err.println("WARN: failed to write GITHUB_STEP_SUMMARY: " + e.getMessage());
            }
        }

        try (Writer w = new FileWriter("verapdf-summary.md")) {
            w.write(md.toString());
        } catch (IOException e) {
            System.err.println("WARN: failed to write verapdf-summary.md: " + e.getMessage());
        }
    }
}

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
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * Self-contained veraPDF compliance gate.
 *
 * Usage: Validate &lt;pdf-or-dir&gt; &lt;flavour[,flavour...]&gt;
 *   e.g. Validate generated-pdfs ua1,1b
 *
 * Validates every PDF (a single file, or every *.pdf in a directory)
 * against every requested flavour. Prints a per-file/flavour verdict and
 * the failing clause ids. Exits 0 only if EVERY file is compliant with
 * EVERY flavour; exits 1 otherwise (so it gates CI).
 *
 * Also writes a Markdown summary table to $GITHUB_STEP_SUMMARY (if set)
 * and to ./verapdf-summary.md, so CI can surface results in the run
 * summary and as a PR comment without re-parsing stdout.
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

        List<PDFAFlavour> flavours = new ArrayList<>();
        for (String f : args[1].split(",")) {
            flavours.add(PDFAFlavour.fromString(f.trim()));
        }

        VeraGreenfieldFoundryProvider.initialise();
        boolean allPass = true;
        List<String[]> summaryRows = new ArrayList<>();

        for (File pdf : pdfs) {
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

    /**
     * Writes a Markdown results table to $GITHUB_STEP_SUMMARY (appended, if
     * the env var is set) and to ./verapdf-summary.md (overwritten), so the
     * CI job summary and a sticky PR comment can both show the same table
     * without re-parsing stdout.
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FishBench
{
    public partial class MainForm : Form
    {
        static string browseExeForm()
        {
            OpenFileDialog dia = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Executable files |*.exe"
            };
            return dia.ShowDialog() == DialogResult.OK ? dia.FileName : "";
        }

        static string removeQu(string file)
        {
            return (file[0] == '\"' && file[file.Length - 1] == '\"') ? file.Substring(1, file.Length - 2) : file;
        }

        string finishedMask = "Finished: {0}/{1}";
        FishSettings settings = new FishSettings("FishBenchSettings.txt");
        bool validPathBase, validPathStockfish;
        Tester t;
        public MainForm()
        {
            validPathBase = validPathStockfish = false;
            InitializeComponent();
            baseLocationText.Text = settings["base_location"];
            stockfishLocationText.Text = settings["stockfish_location"];
            int result;
            int.TryParse(settings["amount_test"], out result);
            try { amountTestNumeric.Value = result; }
            catch { amountTestNumeric.Value = 5; }
            t = new Tester();
            initHtml();
        }

        private void locationText_TextChanged(object sender, EventArgs e)
        {
            TextBox _sender = sender as TextBox;
            if (_sender == null) return;
            bool valid = false;
            string text = removeQu(_sender.Text);
            if(text != _sender.Text)
            {
                _sender.Text = text;
                return;
            }
            if (text == "")
                _sender.BackColor = SystemColors.Window;
            else if (text.EndsWith(".exe") && File.Exists(text))
            {
                _sender.BackColor = Color.PaleGreen;
                valid = true;
            }
            else
                _sender.BackColor = Color.LightCoral;
            if (_sender == baseLocationText)
                validPathBase = valid;
            else if (_sender == stockfishLocationText)
                validPathStockfish = valid;
        }

        private void baseBrowseButton_Click(object sender, EventArgs e)
        {
            string res = browseExeForm();
            if (res != "")
                baseLocationText.Text = res;
        }

        private void stockfishBrowseButton_Click(object sender, EventArgs e)
        {
            string res = browseExeForm();
            if (res != "")
                stockfishLocationText.Text = res;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if(!validPathStockfish || !validPathBase) 
            {
                MessageBox.Show("Please enter valid executable path");
                return;
            }
            saveSettings();
            t = new Tester(baseLocationText.Text, stockfishLocationText.Text);
            t.Amount = (int)amountTestNumeric.Value;

            setResult(0, 0, 0, 0, 0, 0, 0, 0, 0);
            progressBar.Value = 0;

            progressMessage.Text = string.Format(finishedMask, 0, (int)amountTestNumeric.Value * 2);
            terminateButton.Enabled = true;
            startButton.Enabled = false;
            t.TestFinished += delegate
            {
                progressBar.SetAsync("Value", (int)t.PercentCompleted);
                progressMessage.SetAsync("Text", string.Format(finishedMask, t.Completed, t.Amount));
                setResult(t.AverageA, t.AverageB, t.AverageDiff, t.StdevA, t.StdevB, t.StdevDiff, t.CompletedEach, 
                    Math.Round(t.p_value, 3), Math.Round(t.speedup, 3));
            };
            t.JobFinished += delegate
            {
                startButton.SetAsync("Enabled", true);
                terminateButton.SetAsync("Enabled", false);
            };
            t.DoJob();
        }

        

        private void saveSettings()
        {
            settings["amount_test"] = ((int)(amountTestNumeric.Value)).ToString();
            settings["stockfish_location"] = stockfishLocationText.Text;
            settings["base_location"] = baseLocationText.Text;
        }

        private void terminateButton_Click(object sender, EventArgs e)
        {
            if(t != null)
            {
                t.AbortJob();
                t.WaitJobEnd();
                terminateButton.Enabled = false;
                startButton.Enabled = true;
            }
        }

        string resultLineFormat = "    {0,-8}{1,-10}{2,-10}{3,-10}\r\n";
        private void setResult(object baseMean, object testMean, object diffMean,
            object baseStdev, object testStdev, object diffStdev,
            object testAmount, object pval, object speedup)
        {
            resultsBox.SetAsync("Text",
                "Results for " + testAmount.ToString() + " tests for each version:\r\n\r\n" +
                string.Format(resultLineFormat, "", "Base", "Test", "Diff") +
                string.Format(resultLineFormat, "Mean", baseMean, testMean, diffMean) +
                string.Format(resultLineFormat, "StDev", baseStdev, testStdev, diffStdev) +
                "\r\np-value: " + pval.ToString() + "\r\nspeedup: " + speedup.ToString());
            htmlText = String.Format(htmlFormat, testAmount, baseMean, testMean, diffMean, 
                baseStdev, testStdev, diffStdev, pval, speedup);
        }
        private string htmlText, htmlFormat;
        private void copyButton_Click(object sender, EventArgs e)
        {
            DataObject obj = HtmlToDataObject(htmlText);
            obj.SetText(resultsBox.Text);
            Clipboard.SetDataObject(obj);
            copyButton.BackColor = Color.PaleGreen;
            Timer t = new Timer();
            t.Tick += delegate
            {
                t.Stop();
                copyButton.BackColor = SystemColors.Control;
            };
            t.Interval = 400;
            t.Start();
        }
        private void initHtml()
        {
            htmlFormat = "";
            htmlFormat += "Results for {0} tests for each version:<br>";
            htmlFormat += "<table dir=\"ltr\">";
            htmlFormat += "<tr><td></td><td>Base</td><td>Test</td><td>Diff</td></tr>";
            htmlFormat += "<tr><td>Mean</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>";
            htmlFormat += "<tr><td>StDev</td><td>{4}</td><td>{5}</td><td>{6}</td></tr>";
            htmlFormat += "</table>";
            htmlFormat += "p-value: {7:P2}<br>";
            htmlFormat += "speedup: {8:P2}<br>";
            htmlFormat = htmlFormat.Replace("<td", "<td style=\"padding: 6px\"");
            htmlText = string.Format(htmlFormat, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }
        public static DataObject HtmlToDataObject(string html)
        {
            Encoding enc = Encoding.UTF8;

            string begin = "Version:0.9\r\nStartHTML:{0:000000}\r\nEndHTML:{1:000000}"
              + "\r\nStartFragment:{2:000000}\r\nEndFragment:{3:000000}\r\n";

            string html_begin = "<html>\r\n<head>\r\n"
              + "<meta http-equiv=\"Content-Type\""
              + " content=\"text/html; charset=" + enc.WebName + "\">\r\n"
              + "<title>HTML clipboard</title>\r\n</head>\r\n<body>\r\n"
              + "<!--StartFragment-->";

            string html_end = "<!--EndFragment-->\r\n</body>\r\n</html>\r\n";

            string begin_sample = String.Format(begin, 0, 0, 0, 0);

            int count_begin = enc.GetByteCount(begin_sample);
            int count_html_begin = enc.GetByteCount(html_begin);
            int count_html = enc.GetByteCount(html);
            int count_html_end = enc.GetByteCount(html_end);

            string html_total = String.Format(
              begin
              , count_begin
              , count_begin + count_html_begin + count_html + count_html_end
              , count_begin + count_html_begin
              , count_begin + count_html_begin + count_html
              ) + html_begin + html + html_end;

            DataObject obj = new DataObject();
            obj.SetData(DataFormats.Html, new MemoryStream(
              enc.GetBytes(html_total)));

            return obj;
        }
    }
}

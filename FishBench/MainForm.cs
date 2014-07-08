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
            baseAverageText.Text = "0";
            stockfishAverageText.Text = "0";
            progressBar.Value = 0;
            progressMessage.Text = string.Format(finishedMask, 0, (int)amountTestNumeric.Value * 2);
            terminateButton.Enabled = true;
            startButton.Enabled = false;
            t.TestFinished += delegate
            {
                baseAverageText.SetAsync("Text", t.AverageA.ToString());
                stockfishAverageText.SetAsync("Text", t.AverageB.ToString());
                progressBar.SetAsync("Value", (int)t.PercentCompleted);
                progressMessage.SetAsync("Text", string.Format(finishedMask, t.Completed, t.Amount));
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
    }
}

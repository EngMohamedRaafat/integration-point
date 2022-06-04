using Bunifu.Framework.UI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IntergrationPoint
{
    public partial class Form1 : Form
    {
        Bunifu.Framework.UI.BunifuFlatButton previousButton;
        FolderBrowserDialog folderDialog;
        OpenFileDialog fileDialog;
        SaveFileDialog saveFileDialog;
        double similarityThreshold;
        string datePattern = @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} \S{3} \[\S{3}.\d{4}.\d{4}\S{1}\]";
        List<ServerLogInfo> serverLogInfos;

        public Form1()
        {
            InitializeComponent();
            tabControl1.Size = new Size(tabControl1.Width, tabControl1.Height + 20);
            folderDialog = new FolderBrowserDialog();
            fileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();

            cr_datepicker.Value = DateTime.Now;
            percentage_txt.Text = (ConfigurationManager.GetSection("LogAnalyserSettings") as NameValueCollection)["DefaultSimilarityPercentage"];
        }

        private void menuButton_Click(object sender, EventArgs e)
        {
            indicator.Top = ((Control)sender).Top;
            tabControl1.SelectTab(((Control)sender).Tag.ToString());

            if (previousButton != null)
                previousButton.Normalcolor = Color.White;

            previousButton = ((BunifuFlatButton)sender);
            previousButton.Normalcolor = Color.WhiteSmoke;
        }

        private void exit_btn_Click(object sender, EventArgs e) => Application.Exit();

        private void browse_btn_Click(object sender, EventArgs e)
        {
            if (radioBtn_File.Checked)
            {
                if (fileDialog.ShowDialog() == DialogResult.OK)
                    path_txt.Text = fileDialog.FileName;
            }
            else if (radioBtn_folder.Checked)
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                    path_txt.Text = folderDialog.SelectedPath;
            }
        }

        private void similarity_cb_OnChange(object sender, EventArgs e)
        {
            percentage_txt.Enabled = similarity_cb.Checked;
            percentage_lbl.Enabled = similarity_cb.Checked;
        }

        private void similarity_lbl_Click(object sender, EventArgs e)
        {
            similarity_cb.Checked = !similarity_cb.Checked;
            similarity_cb_OnChange(sender, e);
        }

        private void start_btn_Click(object sender, EventArgs e)
        {
            //string[] lines = File.ReadLines(path, Encoding.UTF8);
            //string testCaseName = fileDialog.SafeFileName;
            analyzerProgress.Maximum = File.ReadLines(path_txt.Text).Count();
            analyzerProgress.Value = 0;
            similarityThreshold = double.Parse(percentage_txt.Text);

            serverLogInfos = new List<ServerLogInfo>();
            string path = path_txt.Text;
            Regex rgx = new Regex(datePattern);

            string log = "";
            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                if (rgx.IsMatch(line)) // New log
                {
                    if (String.IsNullOrEmpty(log))
                    {
                        // Start new log accumulator
                        log = line;
                    }
                    else
                    {
                        // Process previous log
                        processLog(log);

                        // Clear log
                        log = line;
                    }
                }
                else //belongs to the last log
                {
                    log += '\n' + line;
                }
                analyzerProgress.Value++;
            }
            processLog(log);
            analyzerProgress.Value++;

            displayData();
        }

        private void displayData()
        {
            dataGrid1.Rows.Clear();
            int idx = 0;
            foreach (ServerLogInfo logInfo in serverLogInfos)
            {
                dataGrid1.Rows.Add();
                dataGrid1[0, idx].Value = logInfo.dateTime;
                dataGrid1[1, idx].Value = logInfo.logInfo;
                dataGrid1[2, idx].Value = logInfo.numOfAnonymouscaller.ToString();

                //bool isFirst = true;
                foreach (ServiceInfo serviceInfo in logInfo.servicesInfoList)
                {
                    //if (!isFirst)
                    //{
                    //    dataGrid1.Rows.Add();
                    //    idx++;
                    //    dataGrid1.Rows[idx].Cells[5] = new DataGridViewTextBoxCell(); // working

                    //}
                    //isFirst = false;

                    //dataGrid1[3, idx].Value = serviceInfo.name;
                    //dataGrid1[4, idx].Value = serviceInfo.count.ToString();

                    dataGrid1[3, idx].Value += serviceInfo.name + Environment.NewLine;
                    dataGrid1[4, idx].Value += serviceInfo.count.ToString() + Environment.NewLine;
                }
                idx++;
            }
        }

        private void processLog(string fullLog)
        {
            string log = fullLog.Substring(fullLog.IndexOf(']') + 2); // log without datetime
            ServerLogInfo serverLogInfo = new ServerLogInfo
            {
                dateTime = fullLog.Substring(0, fullLog.IndexOf(']') + 2).Trim(),
            };
            string processNamePattern = @"^\S+ -\s+\S+ -";
            Regex rgx = new Regex(processNamePattern);
            if (rgx.IsMatch(log)) // to check if log has processName
            {
                int logStartIdx = getLogIndex(log);
                string serviceInfo = log.Substring(0, logStartIdx - 1).Trim();
                serverLogInfo.logInfo = serverLogInfo.removeErrorSignature(log.Substring(logStartIdx + 1).Trim());

                int logIdx = indexOfServerLog(serverLogInfo.logInfo, out double similarityDegree);
                if (logIdx == -1)
                {
                    serverLogInfo.setServiceInfo(serviceInfo);
                    serverLogInfo.addToSimilarLogSet(serverLogInfo.logInfo, similarityDegree);
                    serverLogInfos.Add(serverLogInfo);
                }
                else
                {
                    serverLogInfos[logIdx].setServiceInfo(serviceInfo);
                    serverLogInfos[logIdx].addToSimilarLogSet(serverLogInfo.logInfo, similarityDegree);
                }
            }
            else // log with anonymous owner
            {
                serverLogInfo.logInfo = serverLogInfo.removeErrorSignature(fullLog.Substring(fullLog.IndexOf(']') + 1).Trim());
                int logIdx = indexOfServerLog(serverLogInfo.logInfo, out double similarityDegree);
                if (logIdx == -1)
                {
                    serverLogInfo.numOfAnonymouscaller = 1;
                    serverLogInfo.addToSimilarLogSet(serverLogInfo.logInfo, similarityDegree);
                    serverLogInfos.Add(serverLogInfo);
                }
                else
                {
                    serverLogInfos[logIdx].addToSimilarLogSet(serverLogInfo.logInfo, similarityDegree);
                    serverLogInfos[logIdx].numOfAnonymouscaller++;
                }
            }
        }

        private int getLogIndex(string log)
        {
            bool isAppeared = false;
            for (int i = 0; i < log.Length; i++)
            {
                if (log[i] == '-')
                {
                    if (isAppeared)
                        return i + 1;
                    else
                        isAppeared = true;
                }
            }
            return 0;
        }

        private int indexOfServerLog(string logValue, out double similarityDegree)
        {
            Regex regex = new Regex(@"^\S+-\S+-\S+-\S+-\S+:");
            string str2 = logValue.ToLower();
            bool isStr2Match = regex.IsMatch(str2);
            if (isStr2Match)
            {
                string id = str2.Substring(0, str2.IndexOf(':') + 1);
                str2 = str2.Replace(id, "");
            }
            int idx = 0;
            foreach (var item in serverLogInfos)
            {
                string str1 = item.logInfo.ToLower();
                if (similarity_cb.Checked)
                {
                    if (isStr2Match && regex.IsMatch(str1))
                    {
                        string id = str1.Substring(0, str1.IndexOf(':') + 1);
                        str1 = str1.Replace(id, "");
                    }

                    similarityDegree = LogSimilarity.CalculateSimilarity(str1, str2) * 100;
                    if (similarityDegree >= similarityThreshold)
                        return idx;
                }
                else
                {
                    if (str1.Contains(str2) || str2.Contains(str1))
                    {
                        if (logValue.Length > item.logInfo.Length)
                            serverLogInfos[idx].logInfo = logValue;
                        similarityDegree = -1;
                        return idx;
                    }
                }
                idx++;
            }
            similarityDegree = -1;
            return -1;
        }

        private void dataGrid1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGrid1.Columns[e.ColumnIndex].Name == "similarLogs")
            {
                dataGrid2.Rows.Clear();
                foreach (string log in serverLogInfos[e.RowIndex].similarLog)
                    dataGrid2.Rows.Add(log);
                tabControl1.SelectTab(SimilarLogsTab);
            }
        }

        private void similarLogTab_close_btn_Click(object sender, EventArgs e)
        {
            tabControl1.SelectTab(LogAnalyserTab);
        }

        private void cr_save_btn_Click(object sender, EventArgs e)
        {
            var CRSettings = ConfigurationManager.GetSection("CRSettings") as NameValueCollection;
            string tempDocPath = Directory.GetCurrentDirectory() + CRSettings["TemplateWordFilePath"];

            saveFileDialog.FileName = Path.GetFileName(tempDocPath).Replace(CRSettings["FilePlaceholder"], title_txt.Text.Trim());
            saveFileDialog.Title = "Save Document As";
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.DefaultExt = "docx";
            saveFileDialog.Filter = "Word Document (*.docx;*.doc)|*.docx;*.doc";
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                wait_lbl.Visible = true;
                wait_lbl.Text = "Please wait ...";
                bool isCreated = WordManager.CreateWordDocument(tempDocPath,
                    saveFileDialog.FileName,
                    title_txt.Text,
                    cr_datepicker.Value.ToString(CRSettings["DateFormat"]),
                    description_txt.Text,
                    purpose_txt.Text,
                    currBehavior_txt.Text);

                wait_lbl.Text = "Done!";

                if (isCreated)
                    MessageBox.Show("File Created Successfully");
                else
                    MessageBox.Show("File not Found!");
            }
            wait_lbl.Visible = false;
        }
    }
}

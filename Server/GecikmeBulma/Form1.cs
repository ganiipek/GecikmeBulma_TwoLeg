using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GecikmeBulma
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            UI.UIManager._form1 = this;
            UI.UIManager.Initialize();

            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView2.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView3.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView4.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            foreach(DataGridViewColumn column in dataGridView4.Columns)
            {
                column.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UI.UIManager.thread = false;
            UI.UIManager.ThreadStop();
            Application.Exit();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        public void dataGridView1_AddItem(Trade.Pair pair)
        {
            dataGridView1.BeginInvoke(
                new Action(delegate ()
                {
                    if (pair != null)
                    {
                        int rowId = dataGridView1.Rows.Add(
                            "False",
                            pair.Symbol,
                            pair.Broker.Name,
                            pair.Volume.ToString(),
                            pair.Slippage.ToString(),
                            pair.MinPipDiff.ToString(),
                            pair.Pyramiding.ToString(),
                            pair.TP.ToString(),
                            pair.Offset.ToString(),
                            pair.Digits.ToString(),
                            pair.ContractSize.ToString(),
                            pair.Ask.ToString(),
                            pair.Bid.ToString(),
                            pair.Spread.ToString(),
                            pair.Time.ToString("HH:mm:ss")
                        );

                        DataGridViewRow row = dataGridView1.Rows[rowId];
                        row.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }));
        }

        public void dataGridView1_UpdateItem(Trade.Pair pair)
        {
            dataGridView1.BeginInvoke(
                new Action(delegate ()
                {
                    if (pair != null)
                    {
                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            if (row.Cells[1].Value.ToString() == pair.Symbol && row.Cells[2].Value.ToString() == pair.Broker.Name)
                            {
                                double oldAsk = Convert.ToDouble(row.Cells[11].Value);
                                double oldBid = Convert.ToDouble(row.Cells[12].Value);
                                int oldSpread = Convert.ToInt32(row.Cells[13].Value);

                                row.Cells[11].Value = pair.Ask.ToString();
                                row.Cells[12].Value = pair.Bid.ToString();
                                row.Cells[13].Value = pair.Spread.ToString();
                                row.Cells[14].Value = pair.Time.ToString("HH:mm:ss");

                                if (pair.Ask > oldAsk) row.Cells[11].Style.ForeColor = Color.Green;
                                else if (pair.Ask < oldAsk) row.Cells[11].Style.ForeColor = Color.Red;

                                if (pair.Bid > oldBid) row.Cells[12].Style.ForeColor = Color.Green;
                                else if (pair.Bid < oldBid) row.Cells[12].Style.ForeColor = Color.Red;

                                if (pair.Spread > oldSpread) row.Cells[13].Style.ForeColor = Color.Green;
                                else if (pair.Spread < oldSpread) row.Cells[13].Style.ForeColor = Color.Red;

                                break;
                            }
                        }
                    }
                }));
        }

        public void dataGridView1_RemoveItem(Trade.Pair pair)
        {
            dataGridView1.BeginInvoke(
                new Action(delegate ()
                {
                    if(pair != null)
                    {
                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            if (row.Cells[1].Value.ToString() == pair.Symbol && row.Cells[2].Value.ToString() == pair.Broker.Name)
                            {
                                dataGridView1.Rows.RemoveAt(row.Index);
                                break;
                            }
                        }
                    }
                }));
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int columnIndex = e.ColumnIndex;
            int rowIndex = e.RowIndex;

            if (rowIndex < 0) return;

            string symbol = dataGridView1.Rows[rowIndex].Cells[1].Value.ToString();
            string broker = dataGridView1.Rows[rowIndex].Cells[2].Value.ToString();

            try
            {
                if (dataGridView1.Columns[columnIndex].HeaderText == "Enabled")
                {
                    bool newValue = Convert.ToBoolean(dataGridView1.Rows[rowIndex].Cells[columnIndex].Value);

                    UI.UIManager.PairSetActive(symbol, broker, newValue);
                }
                else if (dataGridView1.Columns[columnIndex].HeaderText == "Lots")
                {
                    double newValue = Convert.ToDouble(dataGridView1.Rows[rowIndex].Cells[columnIndex].Value);

                    double correctValue = UI.UIManager.PairSetVolume(symbol, broker, newValue);

                    if (correctValue == newValue)
                    {
                        dataGridView1.Rows[rowIndex].Cells[columnIndex].Style.BackColor = Color.White;
                    }
                    else
                    {
                        dataGridView1.Rows[rowIndex].Cells[columnIndex].Value = correctValue;
                        dataGridView1.Rows[rowIndex].Cells[columnIndex].Style.BackColor = Color.Red;
                    }
                }
                else if (dataGridView1.Columns[columnIndex].HeaderText == "Slippage")
                {
                    int newValue = Convert.ToInt32(dataGridView1.Rows[rowIndex].Cells[columnIndex].Value);

                    UI.UIManager.PairSetSlippage(symbol, broker, newValue);
                }
                else if (dataGridView1.Columns[columnIndex].HeaderText == "Min Diff")
                {
                    int newValue = Convert.ToInt32(dataGridView1.Rows[rowIndex].Cells[columnIndex].Value);

                    UI.UIManager.PairSetMinDiff(symbol, broker, newValue);
                }
                else if (dataGridView1.Columns[columnIndex].HeaderText == "Offset")
                {
                    int newValue = Convert.ToInt32(dataGridView1.Rows[rowIndex].Cells[columnIndex].Value);

                    UI.UIManager.PairSetOffset(symbol, broker, newValue);
                }
                else if (dataGridView1.Columns[columnIndex].HeaderText == "TP")
                {
                    double newValue = Convert.ToDouble(dataGridView1.Rows[rowIndex].Cells[columnIndex].Value);

                    UI.UIManager.PairSetTP(symbol, broker, newValue);
                }
                else if (dataGridView1.Columns[columnIndex].HeaderText == "Pyramid")
                {
                    int newValue = Convert.ToInt32(dataGridView1.Rows[rowIndex].Cells[columnIndex].Value);

                    UI.UIManager.PairSetPyramiding(symbol, broker, newValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        public void dataGridView2_AddItem(Trade.Broker broker)
        {
            dataGridView2.BeginInvoke(
                new Action(delegate ()
                {
                    int rowId = dataGridView2.Rows.Add(
                        broker.Name,
                        broker.PlatformId.ToString(),
                        broker.Latency.ToString() + " ms",
                        broker.Connected.ToString(),
                        broker.Balance.ToString(),
                        broker.Profit.ToString(),
                        broker.AutoTrade.ToString()
                    );

                    DataGridViewRow row = dataGridView2.Rows[rowId];
                    row.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

                    if (broker.Connected) row.Cells[3].Style.BackColor = Color.Green;
                    else row.Cells[3].Style.ForeColor = Color.Red;
                }));
        }

        public void dataGridView2_UpdateItem(Trade.Broker broker)
        {
            dataGridView2.BeginInvoke(
                new Action(delegate ()
                {
                    foreach (DataGridViewRow row in dataGridView2.Rows)
                    {
                        if (row.Cells[0].Value.ToString() == broker.Name && row.Cells[1].Value.ToString() == broker.PlatformId.ToString())
                        {
                            row.Cells[2].Value = broker.Latency.ToString() + " ms";
                            row.Cells[3].Value = broker.Connected.ToString();
                            row.Cells[4].Value = broker.Balance.ToString();
                            row.Cells[5].Value = broker.Profit.ToString();

                            if (broker.Connected) row.Cells[3].Style.BackColor = Color.Green;
                            else row.Cells[3].Style.ForeColor = Color.Red;

                            break;
                        }
                    }
                }));
        }

        public void dataGridView2_RemoveItem(Trade.Broker broker)
        {
            dataGridView2.BeginInvoke(
                new Action(delegate ()
                {
                    foreach (DataGridViewRow row in dataGridView2.Rows)
                    {
                        if (row.Cells[0].Value.ToString() == broker.Name)
                        {
                            dataGridView2.Rows.RemoveAt(row.Index);
                            break;
                        }
                    }
                }));
        }

        private void dataGridView2_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int columnIndex = e.ColumnIndex;
            int rowIndex = e.RowIndex;

            if (rowIndex < 0) return;

            string broker = dataGridView2.Rows[rowIndex].Cells[0].Value.ToString();
            int platformId = Convert.ToInt32(dataGridView2.Rows[rowIndex].Cells[1].Value);

            if (dataGridView2.Columns[columnIndex].HeaderText == "Trading")
            {
                bool newValue = Convert.ToBoolean(dataGridView2.Rows[rowIndex].Cells[columnIndex].Value);

                UI.UIManager.BrokerSetActive(broker, platformId, newValue);
            }
        }

        public void dataGridView4_AddItem(Trade.Arbitrage arbitrage)
        {
            dataGridView4.BeginInvoke(
                new Action(delegate ()
                {
                    bool isExist = false;

                    foreach (DataGridViewRow row in dataGridView4.Rows)
                    {
                        if (row.Cells[0].Value.ToString() == arbitrage.Id.ToString())
                        {
                            isExist = true;
                            break;
                        }
                    }
                        
                    if(!isExist)
                    {
                        string askBrokerName = arbitrage.AskPair.Broker.Name;
                        string bidBrokerName = arbitrage.BidPair.Broker.Name;

                        if (askBrokerName.Length > 10) askBrokerName = askBrokerName.Substring(0, 10);
                        if (bidBrokerName.Length > 10) bidBrokerName = bidBrokerName.Substring(0, 10);

                        int rowId = dataGridView4.Rows.Add(
                            arbitrage.Id.ToString(),
                            arbitrage.AskPair.Symbol,
                            askBrokerName + Environment.NewLine + bidBrokerName,
                            arbitrage.GetTotalLongVolume().ToString() + Environment.NewLine + arbitrage.GetTotalShortVolume().ToString(),
                            arbitrage.Created.ToString("HH:mm:ss"),
                            arbitrage.GetProfit().ToString()
                        );

                        DataGridViewRow dataGridViewRow = dataGridView4.Rows[rowId];
                        dataGridViewRow.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                }));
        }

        public bool dataGridView4_UpdateItem(Trade.Arbitrage arbitrage)
        {
            bool error = true;
            dataGridView4.BeginInvoke(
                new Action(delegate ()
                {
                    foreach (DataGridViewRow row in dataGridView4.Rows)
                    {
                        if (row.Cells[0].Value.ToString() == arbitrage.Id.ToString())
                        {
                            error = false;
                            row.Cells[3].Value = arbitrage.GetTotalLongVolume().ToString() + Environment.NewLine + arbitrage.GetTotalShortVolume().ToString();
                            row.Cells[5].Value = arbitrage.GetProfit().ToString();
                            break;
                        }
                    }
                })
            );

            return !error;
        }

        public void dataGridView4_RemoveItem(Trade.Arbitrage arbitrage)
        {
            dataGridView4.BeginInvoke(
                new Action(delegate ()
                {
                    foreach (DataGridViewRow row in dataGridView4.Rows)
                    {
                        if (row.Cells[0].Value.ToString() == arbitrage.Id.ToString())
                        {
                            dataGridView4.Rows.RemoveAt(row.Index);
                        }
                    }
                }));
        }

        public void listView3_AddItem(DateTime time, LoggerService.LoggerType loggerType, string log)
        {
                listView3.BeginInvoke(
                    new Action(delegate ()
                    {
                        listView3.BeginUpdate();

                        var listViewItem = new ListViewItem(
                            new string[] {
                                time.ToString("dd.MM.yyyy HH:mm:ss.fff"),
                                loggerType.ToString(),
                                log
                            }
                            );
                        listViewItem.UseItemStyleForSubItems = false;

                        listViewItem.SubItems[1].Font = new Font(listViewItem.SubItems[0].Font, FontStyle.Bold);

                        if (loggerType == LoggerService.LoggerType.ERROR) listViewItem.SubItems[1].ForeColor = Color.Red;
                        else if (loggerType == LoggerService.LoggerType.WARNING) listViewItem.SubItems[1].ForeColor = Color.Orange;
                        else if (loggerType == LoggerService.LoggerType.INFO) listViewItem.SubItems[1].ForeColor = Color.DarkBlue;
                        else if (loggerType == LoggerService.LoggerType.DEBUG) listViewItem.SubItems[1].ForeColor = Color.Pink;
                        else if (loggerType == LoggerService.LoggerType.SUCCESS) listViewItem.SubItems[1].ForeColor = Color.Green;

                        listView3.Items.Add(listViewItem);

                        if(checkBox1.Checked) listView3.TopItem = listView3.Items[listView3.Items.Count - 1];

                        listView3.EndUpdate();
                    }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView4.SelectedRows.Count > 0)
            {
                int arbitrageId = Convert.ToInt32(dataGridView4.SelectedRows[0].Cells[0].Value);
                UI.UIManager.arbitrageForceClose(arbitrageId);
            }
        }
    }

    public class ListViewNF : System.Windows.Forms.ListView
    {
        public ListViewNF()
        {
            //Activate double buffering
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            //Enable the OnNotifyMessage event so we get a chance to filter out 
            // Windows messages before they get to the form's WndProc
            this.SetStyle(ControlStyles.EnableNotifyMessage, true);
        }

        protected override void OnNotifyMessage(Message m)
        {
            //Filter out the WM_ERASEBKGND message
            if (m.Msg != 0x14)
            {
                base.OnNotifyMessage(m);
            }
        }
    }
}

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using libzkfpcsharp;
using Sample;
using System.Drawing.Imaging;
using SAP.Middleware.Connector;
using System.Security.Cryptography;
using System.Data;

namespace Demo
{
    public partial class Form1 : Form
    {
        IntPtr mDevHandle = IntPtr.Zero;
        IntPtr mDBHandle = IntPtr.Zero;
        IntPtr FormHandle = IntPtr.Zero;
        bool bIsTimeToDie = false;
        bool IsRegister = false;
        bool bIdentify = true;
        byte[] FPBuffer;
        int RegisterCount = 0;
        const int REGISTER_FINGER_COUNT = 1;

        byte[][] RegTmps = new byte[3][];
        byte[] RegTmp = new byte[2048];
        byte[] CapTmp = new byte[2048];

        int cbCapTmp = 2048;
        int cbRegTmp = 0;
        int iFid = 1;

        int filesizes;
        int vald;
        int jj;

        private int mfpWidth = 0;
        private int mfpHeight = 0;
        private int mfpDpi = 0;

        const int MESSAGE_CAPTURED_OK = 0x0400 + 6;

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public Form1()
        {
            InitializeComponent();
        }

        private void Init_Mesin()
        {
            cmbIdx.Items.Clear();
            int ret = zkfperrdef.ZKFP_ERR_OK;
            if ((ret = zkfp2.Init()) == zkfperrdef.ZKFP_ERR_OK)
            {
                int nCount = zkfp2.GetDeviceCount();
                if (nCount > 0)
                {
                    for (int i = 0; i < nCount; i++)
                    {
                        cmbIdx.Items.Add(i.ToString());
                    }
                    cmbIdx.SelectedIndex = 0;
                    bnInit.Enabled = false;


                }
                else
                {
                    zkfp2.Terminate();
                    txtStatus.Text = "No device connected!";

                }
            }
            else
            {
                MessageBox.Show("Initialize fail, ret=" + ret + " !");
            }
        }

        private void open_device()
        {
            int ret = zkfp.ZKFP_ERR_OK;
            if (IntPtr.Zero == (mDevHandle = zkfp2.OpenDevice(cmbIdx.SelectedIndex)))
            {
                MessageBox.Show("OpenDevice fail");
                return;
            }
            if (IntPtr.Zero == (mDBHandle = zkfp2.DBInit()))
            {
                MessageBox.Show("Init DB fail");
                zkfp2.CloseDevice(mDevHandle);
                mDevHandle = IntPtr.Zero;
                return;
            }
            bnInit.Enabled = false;

            bnEnroll.Enabled = true;

            RegisterCount = 0;
            cbRegTmp = 0;
            iFid = 1;
            for (int i = 0; i < 3; i++)
            {
                RegTmps[i] = new byte[2048];
            }
            byte[] paramValue = new byte[4];
            int size = 4;
            zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpWidth);

            size = 4;
            zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpHeight);

            FPBuffer = new byte[mfpWidth * mfpHeight];

            size = 4;
            zkfp2.GetParameters(mDevHandle, 3, paramValue, ref size);
            zkfp2.ByteArray2Int(paramValue, ref mfpDpi);

            Thread captureThread = new Thread(new ThreadStart(DoCapture));
            captureThread.IsBackground = true;
            captureThread.Start();
            bIsTimeToDie = false;

            txtStatus.Text = "Connected Succesful";

        }

        private void bnInit_Click(object sender, EventArgs e)
        {
            Init_Mesin(); //Initialization Fingerprint
            open_device(); //Open Devices
            cbRegTmp = 1;
            if (bIdentify)
            {
                bIdentify = false;

                txtStatus.Text = "Please press your finger!";
            }

        }


        private void DoCapture()
        {
            while (!bIsTimeToDie)
            {
                cbCapTmp = 2048;
                int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);
                if (ret == zkfp.ZKFP_ERR_OK)
                {
                    SendMessage(FormHandle, MESSAGE_CAPTURED_OK, IntPtr.Zero, IntPtr.Zero);
                }
                Thread.Sleep(200);
            }
        }

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case MESSAGE_CAPTURED_OK:
                    {
                        MemoryStream ms = new MemoryStream();
                        BitmapFormat.GetBitmap(FPBuffer, mfpWidth, mfpHeight, ref ms);
                        Bitmap bmp = new Bitmap(ms);
                        this.picFPImg.Image = bmp;

                        String strShow = zkfp2.BlobToBase64(CapTmp, cbCapTmp);


                        if (IsRegister)
                        {
                            int ret = zkfp.ZKFP_ERR_OK;
                            int fid = 0, score = 0;
                            ret = zkfp2.DBIdentify(mDBHandle, CapTmp, ref fid, ref score);
                            if (zkfp.ZKFP_ERR_OK == ret)
                            {

                                txtStatus.Text = "This finger was already register";
                                return;
                            }

                            if (RegisterCount > 0 && zkfp2.DBMatch(mDBHandle, CapTmp, RegTmps[RegisterCount - 1]) <= 0)
                            {


                                txtStatus.Text = "Please press the same finger 1 times for the enrollment.";
                                return;
                            }

                            Array.Copy(CapTmp, RegTmps[RegisterCount], cbCapTmp);
                            String strBase64 = zkfp2.BlobToBase64(CapTmp, cbCapTmp);


                            byte[] blob = zkfp2.Base64ToBlob(strBase64);
                            RegisterCount++;

                            Save_Database(strBase64);

                            if (RegisterCount >= REGISTER_FINGER_COUNT)
                            {

                                txtStatus.Text = "Enroll succesfully";

                                IsRegister = false;
                                cbRegTmp = 1;
                                return;
                            }
                            else
                            {
                                txtStatus.Text = "You need to press the " + (REGISTER_FINGER_COUNT - RegisterCount) + " times fingerprint";
                            }
                        }
                        else
                        {
                            if (cbRegTmp <= 0)
                            {

                                txtStatus.Text = "Please register your finger first!";
                                return;
                            }
                            if (bIdentify)
                            {
                                int ret = zkfp.ZKFP_ERR_OK;
                                int fid = 0, score = 0;
                                ret = zkfp2.DBIdentify(mDBHandle, CapTmp, ref fid, ref score);
                                if (zkfp.ZKFP_ERR_OK == ret)
                                {

                                    txtStatus.Text = "Identify succesfully, fid= " + fid + ",score=" + score + "!";
                                    return;
                                }
                                else
                                {

                                    txtStatus.Text = "Identify failed, ret= " + ret;
                                    return;
                                }

                            }
                            else
                            {


                                RfcConfigParameters parameters = new RfcConfigParameters();

                                parameters[RfcConfigParameters.Name] = "JMDEV";
                                parameters[RfcConfigParameters.User] = "ABY_RACHMAD";
                                parameters[RfcConfigParameters.Password] = "rachmad211282";
                                parameters[RfcConfigParameters.Client] = "300";
                                parameters[RfcConfigParameters.Language] = "EN";
                                parameters[RfcConfigParameters.AppServerHost] = "erpappdev";
                                parameters[RfcConfigParameters.SystemNumber] = "03";

                                RfcDestination SapRfcDestination = RfcDestinationManager.GetDestination(parameters);
                                RfcSessionManager.BeginContext(SapRfcDestination);

                                SapRfcDestination.Ping();
                                IRfcFunction function = null;

                                try
                                {

                                    function = SapRfcDestination.Repository.CreateFunction("ZFM_TABLE_FINGER");
                                    function.SetValue("MODE", "S");

                                    IRfcTable gt_table = function.GetTable("EX_FINGERPRINT");

                                    function.Invoke(SapRfcDestination);

                                    jj = 0;
                                    foreach (IRfcStructure row in gt_table)
                                    {

                                        jj = jj + 1;
                                        byte[] blob1 = Convert.FromBase64String(Convert.ToString(row.GetValue(0)));

                                        String strBase64 = zkfp2.BlobToBase64(CapTmp, cbCapTmp);

                                        byte[] blob2 = Convert.FromBase64String(strBase64.Trim());

                                        int ret = zkfp2.DBMatch(mDBHandle, blob1, blob2);
                                        if (ret > 0)
                                        {

                                            txtStatus.Text = "Selamat Datang!";

                                            vald = ret;
                                            break;
                                        }
                                        else
                                        {
                                            txtStatus.Text = "Maaf and tidak dikenali";
                                        }
                                        zkfp2.DBClear(mDBHandle);
                                    }

                                    if (vald > 0)
                                    {
                                        function = SapRfcDestination.Repository.CreateFunction("ZFM_TABLE_FINGER");
                                        function.SetValue("MODE", "L");
                                        function.SetValue("INDX", jj);
                                        function.Invoke(SapRfcDestination);
                                    }

                                }
                                catch (Exception ex)
                                {
                                    System.Windows.Forms.MessageBox.Show("Error: " + ex.Message);
                                }

                                RfcSessionManager.EndContext(SapRfcDestination);
                                SapRfcDestination = null;



                            }
                        }
                    }
                    break;

                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FormHandle = this.Handle;
        }

        private void bnClose_Click(object sender, EventArgs e)
        {
            bIsTimeToDie = true;
            RegisterCount = 0;
            Thread.Sleep(1000);
            zkfp2.CloseDevice(mDevHandle);
            bnInit.Enabled = false;

            bnEnroll.Enabled = false;


        }

        private void bnEnroll_Click(object sender, EventArgs e)
        {
            if (!IsRegister)
            {
                IsRegister = true;
                RegisterCount = 0;
                cbRegTmp = 0;
                // textRes.AppendText("Please press your finger 3 times!\n");
                txtStatus.Text = "Please press your finger 1 times!";
            }
        }




        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            zkfp2.Terminate();
            cbRegTmp = 0;
            bIsTimeToDie = true;

            bnInit.Enabled = true;

            bnEnroll.Enabled = false;

            this.Close();
        }



        private static void Save_Database(string fingerprint)
        {
            RfcConfigParameters parameters = new RfcConfigParameters();

            parameters[RfcConfigParameters.Name] = "JMDEV";
            parameters[RfcConfigParameters.User] = "ABY_RACHMAD";
            parameters[RfcConfigParameters.Password] = "rachmad211282";
            parameters[RfcConfigParameters.Client] = "300";
            parameters[RfcConfigParameters.Language] = "EN";
            parameters[RfcConfigParameters.AppServerHost] = "erpappdev";
            parameters[RfcConfigParameters.SystemNumber] = "03";

            RfcDestination SapRfcDestination = RfcDestinationManager.GetDestination(parameters);
            RfcSessionManager.BeginContext(SapRfcDestination);

            SapRfcDestination.Ping();
            IRfcFunction function = null;

            try
            {

                function = SapRfcDestination.Repository.CreateFunction("ZFM_TABLE_FINGER");
                function.SetValue("FINGERPRINT", fingerprint);
                function.SetValue("MODE", "R");


                function.Invoke(SapRfcDestination);

            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error: " + ex.Message);
            }

            RfcSessionManager.EndContext(SapRfcDestination);
            SapRfcDestination = null;
        }




    }
}


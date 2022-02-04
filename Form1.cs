﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using GroupDocs.Parser.Data;
using GroupDocs.Parser.Options;
using GroupDocs.Parser;
using System.Net.Http;
using System.Threading;
using System.Resources;

namespace SampleUI_SamsungAGV
{
    public partial class Form1 : Form
    {
        System.Drawing.Point location = System.Drawing.Point.Empty;
        private System.Drawing.Point _mouseLoc;
        private static string m_exePath = string.Empty;
        public List<AGVCallingModel> AGVData = new List<AGVCallingModel>();
        public List<AGVStatusModel> AGVStatus = new List<AGVStatusModel>();
        public List<AGVErrorModel> AGVError = new List<AGVErrorModel>();
        public bool writeFlag = false, rt2 = false, endFlag = false, moveTimer =false;
        public int xAgv, yAgv, interval = 5, cntStop = 0, cntStop4 = 0, counterLog, updateError = 0;
        public int testButton = 0, agvAddress = 1, agv2Address =2, waitingTime = 0, moveCnt = 0;
        public string agvState, agvName = "AGV-1",agvTime, agvStatus, agvRoute, agvRfid, statusDelivery, obsCode;
        public string agv2State, agv2Name = "AGV-2", agv2Time, agv2Status, agv2Route, agv2Rfid, statusDelivery2, obsCode2;
        public string readType;
        public long jobId;
        public double offTime,offTime2;

        //public string url = "http://localhost:8000/req";
        public string url = "http://172.16.101.203:8000/req";
        //public string url = "http://10.10.100.100:8000/req";
        //public string url = "http://192.168.77.220:8000/req";
        private double batScale(double value, double min, double max, double minScale, double maxScale)
        {
            double scaled = minScale + (double)(value - min) / (max - min) * (maxScale - minScale);
            return scaled;
        }
        private void activeLine(bool l1, bool l2, bool l3, bool l4, bool l5, bool l6)
        {
            send1.Visible = l1; send2.Visible = l2; send3.Visible = l3; send4.Visible = l4; send5.Visible = l5; send6.Visible = l6;
            standby1.Visible = !l1; standby2.Visible = !l2; standby3.Visible = !l3; standby4.Visible = !l4; standby5.Visible = !l5; standby6.Visible = !l6;
            wip1.Visible = l1; wip2.Visible = l2; wip3.Visible = l3; wip4.Visible = l4; wip5.Visible = l5; wip6.Visible = l6;
        }
        class RequestData
        {
            public string command { get; set; }
            public int serialNumber { get; set; }
        }
        class ResponseData
        {
            public string errMark { get; set; }
            public List<List<dynamic>> msg { get; set; }        // add command checker
            public string command { get; set; }
        }
        class ResponseData2
        {
            public string errMark { get; set; }
            public List<dynamic> msg { get; set; }               
            public string command { get; set; }
        }
        public class AGVDeviceModel
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public AGVDeviceModel(string agvId, string agvName,string status)
            {
                this.ID = agvId;
                this.Name = agvName;
                this.Status=status;
            }
        }
        public class AGV2DeviceModel
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
            public AGV2DeviceModel(string agvId, string agvName, string status)
            {
                this.ID = agvId;
                this.Name = agvName;
                this.Status = status;
            }
        }
        public class AGVStatusModel
        {
            //public string ID { get; set; }
            public string Name { get; set; }
            public string State { get; set; }
            public string Status { get; set; }
            public AGVStatusModel(string agvName,string power, string status)
            {
                //this.ID = agvId;
                this.Name = agvName;
                this.State = power;
                this.Status = status;
            }
        }
        public class AGVCallingModel
        {
            public string Time { get; set; }
            public string Name { get; set; }
            public string Station { get; set; }
            public string Status { get; set; }
            public AGVCallingModel(string time,string agvname, string deliv, string status )
            {
                this.Time = time;
                this.Name = agvname;
                this.Station = deliv;
                this.Status = status;
            }
        }
        public class AGVErrorModel
        {
            public string Time { get; set; }
            public string Name { get; set; }
            public string Error { get; set; }
            public string Obstacle { get; set; }
            public AGVErrorModel(string dateTimeNow, string agvName, string errorCode, string obscode)
            {
                this.Time = dateTimeNow;
                this.Name = agvName;
                this.Error = errorCode;
                this.Obstacle = obscode;
            }
        }

        public class AGV2StatusModel
        {
            //public string ID { get; set; }
            public string Name { get; set; }
            public string State { get; set; }
            public string Status { get; set; }
            public AGV2StatusModel(string agvName, string power, string status)
            {
                //this.ID = agvId;
                this.Name = agvName;
                this.State = power;
                this.Status = status;
            }
        }
        public class AGV2CallingModel
        {
            public string Time { get; set; }
            public string Name { get; set; }
            public string Station { get; set; }
            public string Status { get; set; }
            public AGV2CallingModel(string time, string agvname, string deliv, string status)
            {
                this.Time = time;
                this.Name = agvname;
                this.Station = deliv;
                this.Status = status;
            }
        }
        public class AGV2ErrorModel
        {
            public string Time { get; set; }
            public string Name { get; set; }
            public string Error { get; set; }
            public string Obstacle { get; set; }
            public AGV2ErrorModel(string dateTimeNow, string agvName, string errorCode, string obscode)
            {
                this.Time = dateTimeNow;
                this.Name = agvName;
                this.Error = errorCode;
                this.Obstacle = obscode;
            }
        }

        private async void callAPI()
        {
            //Console.WriteLine("\t Start Call API");
            string[] arrayMachine = new string[] { "SMD_01", "SMD_02", "SMD_03", "SMD_04", "SMD_05", "SMD_06" };
            string[] arrayPosition = new string[] {"ENDING","STANDBY", "GO TO LINE", "GO TO LINE","PARKING AREA", "HOME","WIP AREA", "WIP-IN-1", "WIP-IN-2",
                                                   "WIP-IN-3", "WIP-IN-4","WIP-IN-5", "WIP-OUT-1", "LOW SPEED", "GO TO LINE", "LINE AREA",tbox1.Text,tbox1.Text,
                                                   tbox2.Text,tbox2.Text,tbox3.Text,tbox3.Text,tbox4.Text,tbox4.Text,tbox5.Text,tbox5.Text ,"ENDING"};

            string[] arrayRFID = new string[] { "9","0", "1", "2", "4", "5", "19", "20", "21", "22", "23", "24", "10", "11", "31", "32", "33", "34",
                                                "15", "16","37", "38","39", "40","41", "42" ,"8"};
            int[] arrayRfidLoc_X = new int[] {rfid9.Location.X,0,rfid1.Location.X,rfid2.Location.X,rfid4.Location.X,rfid5.Location.X, rfid19.Location.X,rfid20.Location.X,rfid21.Location.X,
                                              rfid22.Location.X,rfid23.Location.X,rfid24.Location.X,rfid10.Location.X,rfid11.Location.X,rfid31.Location.X,rfid32.Location.X,rfid33.Location.X,
                                              rfid34.Location.X,rfid15.Location.X,rfid16.Location.X,rfid37.Location.X,rfid38.Location.X,rfid39.Location.X,rfid40.Location.X,rfid41.Location.X,
                                              rfid42.Location.X,rfid8.Location.X};
            int[] arrayRfidLoc_Y = new int[] {rfid9.Location.Y,0,rfid1.Location.Y,rfid2.Location.Y,rfid4.Location.Y,rfid5.Location.Y, rfid19.Location.Y,rfid20.Location.Y,rfid21.Location.Y,
                                              rfid22.Location.Y,rfid23.Location.Y,rfid24.Location.Y,rfid10.Location.Y,rfid11.Location.Y,rfid31.Location.Y,rfid32.Location.Y,rfid33.Location.Y,
                                              rfid34.Location.Y,rfid15.Location.Y,rfid16.Location.Y,rfid37.Location.Y,rfid38.Location.Y,rfid39.Location.Y,rfid40.Location.Y,rfid41.Location.Y,
                                              rfid42.Location.Y,rfid8.Location.Y};
            //==================================================================== PHASE 2 ==============================================================// --> 
            string[] arrayRFID_P2 = new string[] { "9","0", "1", "2", "4", "5", "19", "20", "21","22", "23", "24", "10", "11","103","102","101", "31", "32", "33", "34",
                                                   "15", "16","37", "38","39", "40","41", "42","43","44",                                                      //Line 1-6
                                                   "12","13","104","105","106","29","30","45","46","47","48","49","50","51","52","53","54","55","56" ,"8"};    //Line 7-12
            int[] arrayRfidLoc_X_P2 = new int[] {rfid9.Location.X,0,rfid1.Location.X,rfid2.Location.X,rfid4.Location.X,rfid5.Location.X, rfid19.Location.X,rfid20.Location.X,rfid21.Location.X,
                                              rfid22.Location.X,rfid23.Location.X,rfid24.Location.X,rfid10.Location.X,rfid11.Location.X,rfid31.Location.X,rfid32.Location.X,rfid33.Location.X,
                                              rfid34.Location.X,rfid15.Location.X,rfid16.Location.X,rfid37.Location.X,rfid38.Location.X,rfid39.Location.X,rfid40.Location.X,rfid41.Location.X,
                                              rfid42.Location.X,rfid43.Location.X,rfid44.Location.X,rfid12.Location.X,rfid13.Location.X,rfid104.Location.X,rfid105.Location.X,rfid106.Location.X,
                                              rfid29.Location.X,rfid30.Location.X,rfid45.Location.X,rfid46.Location.X,rfid47.Location.X,rfid48.Location.X,rfid49.Location.X,rfid50.Location.X,
                                              rfid51.Location.X,rfid52.Location.X,rfid53.Location.X,rfid54.Location.X,rfid55.Location.X,rfid56.Location.X,rfid8.Location.X};
            int[] arrayRfidLoc_Y_P2 = new int[] {rfid9.Location.Y,0,rfid1.Location.Y,rfid2.Location.Y,rfid4.Location.Y,rfid5.Location.Y, rfid19.Location.Y,rfid20.Location.Y,rfid21.Location.Y,
                                              rfid22.Location.Y,rfid23.Location.Y,rfid24.Location.Y,rfid10.Location.Y,rfid11.Location.Y,rfid31.Location.Y,rfid32.Location.Y,rfid33.Location.Y,
                                              rfid34.Location.Y,rfid15.Location.Y,rfid16.Location.Y,rfid37.Location.Y,rfid38.Location.Y,rfid39.Location.Y,rfid40.Location.Y,rfid41.Location.Y,
                                              rfid42.Location.Y,rfid43.Location.Y,rfid44.Location.Y,rfid12.Location.Y,rfid13.Location.Y,rfid104.Location.Y,rfid105.Location.Y,rfid106.Location.Y,
                                              rfid29.Location.Y,rfid30.Location.Y,rfid45.Location.Y,rfid46.Location.Y,rfid47.Location.Y,rfid48.Location.Y,rfid49.Location.Y,rfid50.Location.Y,
                                              rfid51.Location.Y,rfid52.Location.Y,rfid53.Location.Y,rfid54.Location.Y,rfid55.Location.Y,rfid56.Location.Y,rfid8.Location.Y};
            //==================================================================== PHASE 2 ==============================================================// --> 

            //==================================================================================================================================// --> API1
            ResponseData data = await API("missionC.missionGetActiveList()");
            if (data.errMark == "OK")
            {
                //Console.WriteLine("missionC.missionGetActiveList()");
                List<AGVCallingModel> showData = new List<AGVCallingModel>();
                string lastTIme = "";
                for (int i = 0; i < data.msg.Count; i++)
                {
                    counterLog += 1;
                    agvTime = UnixTimeStampToDateTime(data.msg[i][11]).ToString();
                    statusDelivery = data.msg[i][10];
                    jobId = data.msg[i][0];
                    lastTIme = agvTime;
                    //string text = System.IO.File.ReadAllText(@"D:\WORK\SAMSUNG\SampleUI-SamsungAGV\log.txt");
                    //string[] lines = System.IO.File.ReadAllLines(@"D:\WORK\SAMSUNG\SampleUI-SamsungAGV\log.txt");
                    if (counterLog < 11)
                    {
                        if (statusDelivery == "执行") { statusDelivery = "RUNNING";}
                        else if (statusDelivery == "放弃") { statusDelivery = "HOLD";}
                        else if (statusDelivery == "正常结束") { statusDelivery = "FINISH";}
                        else if (statusDelivery == "错误") { string disc = agvName + " DISCONNECTED"; labelDisconnect.Text = disc; labelDisconnect.Visible = true;}
                        else { Console.WriteLine("Status Delivery : {0}", statusDelivery); labelDisconnect.Visible = false;}

                        //Console.WriteLine("{0} {1} {2} {3} {4} cnt : {5} {6} lastTime : {7}", agvTime, agvName, jobId, statusDelivery, data.msg.Count,
                        //counterLog, writeFlag, lastTIme);
                        //System.Console.WriteLine("Contents of WriteText.txt = \n{0}", text);
                        //foreach (string line in lines)
                        //{
                        //    // Use a tab to indent each line of the file.
                        //    //Console.WriteLine("\n" + line);
                        //}
                        //Console.WriteLine(lines[0]);

                        //// Display the file contents by using a foreach loop.
                        //System.Console.WriteLine("Contents of WriteLines2.txt = \n
                    }
                    else { //Console.WriteLine("API 1 Else ");
                    }

                    if (statusDelivery == "执行")
                    {
                        statusDelivery = "RUNNING";
                        jobId = data.msg[i][0];
                        long[] arrayJobId = new long[] {data.msg[0][0], data.msg[1][0], data.msg[2][0], data.msg[3][0], data.msg[4][0],
                                            data.msg[5][0], data.msg[6][0], data.msg[7][0], data.msg[8][0],data.msg[9][0] };

                        long maxJobid = arrayJobId.Last(), searchJobid = jobId;
                        long indexJob = Array.IndexOf(arrayJobId, searchJobid);
                        string searchString = data.msg[(int)indexJob][1];               //Read first call jobID then search Name
                        int index = Array.IndexOf(arrayMachine, searchString);
                        AGVCallingModel temp = new AGVCallingModel(agvTime, agvName, data.msg[i][1].ToString(), statusDelivery);
                        showData.Add(temp);
                        if (index == 0) { activeLine(true, false, false, false, false, false);}
                        else if (index == 1){ activeLine(false, true, false, false, false, false);}
                        else if (index == 2){ activeLine(false, false, true, false, false, false);}
                        else if (index == 3){ activeLine(false, false, false, true, false, false);}
                        else if (index == 4){ activeLine(false, false, false, false, true, false);}
                        else if (index == 5){ activeLine(false, false, false, false, false, true);}
                        else { activeLine(false, false, false, false, false, false);}
                    }
                    else if (statusDelivery == "放弃")
                    {
                        statusDelivery = "HOLD";
                        AGVCallingModel temp = new AGVCallingModel(agvTime, agvName, data.msg[i][1].ToString(), statusDelivery);
                        //showData.Add(temp);
                    }
                    else if (statusDelivery == "正常结束")
                    {
                        statusDelivery = "FINISH";
                        AGVCallingModel temp = new AGVCallingModel(agvTime, agvName, data.msg[i][1].ToString(), statusDelivery);
                        showData.Add(temp);
                    }
                    else if (statusDelivery == "错误")
                    {
                        statusDelivery = "COM ERR";
                        AGVCallingModel temp = new AGVCallingModel(agvTime, agvName, data.msg[i][1].ToString(), statusDelivery);
                        showData.Add(temp);
                    }
                    else { activeLine(false, false, false, false, false, false);}
                }
                this.gridViewDS.Columns[0].Width = 150;
                this.gridViewDS.Columns[1].Width = 60;
                this.gridViewDS.Columns[2].Width = 60;
                gridViewDS.Invoke((MethodInvoker)delegate { gridViewDS.DataSource = showData; });

            }

            //==================================================================================================================================// --> API2
            data = await API("devC.getCarList()");
            if (data.errMark == "OK")
            {
                List<AGVStatusModel> showData = new List<AGVStatusModel>();
                //List<AGV2StatusModel> showData2 = new List<AGV2StatusModel>();
                for (int i = 0; i < data.msg.Count; i++)
                {
                    double power1 = data.msg[0][7], power2 = data.msg[1][7];
                    batteryLevel1.Value = (int)power1; batValue1.Text = power1.ToString();
                    batteryLevel2.Value = (int)power2; batValue2.Text = power2.ToString();

                    //"车" Read RFID and detail Car activity
                    double dataMovement = data.msg[0][15], dataMovement2 = data.msg[1][15],  dataRute = data.msg[i][31], dataRfid = data.msg[i][33], readAddress = data.msg[0][3], readAddress2 = data.msg[1][3]; ;
                    readType = data.msg[i][2];
                    string searchRFID = dataRfid.ToString();
                    //Console.Write(searchRFID);
                    int indexRFID = Array.IndexOf(arrayRFID, searchRFID);

                    agvStatus = dataMovement.ToString();
                    agv2Status = dataMovement2.ToString();
                    agvRoute = dataRute.ToString();
                    agvRfid = dataRfid.ToString();
                    //Console.WriteLine("API 2 -- agvaddress 1: {0},  2: {1} \nBat-1: {2} Bat-2: {3}",
                    //                    readAddress, readAddress2, power,power2);

                    if ((readAddress == 1 || readAddress2 ==2) && readType == "车")
                    {
                        if (dataMovement == 0 || dataMovement2 == 0 ) { agvStatus = "   STOP"; agv2Status = "STOP"; }
                        else if (dataMovement == 1 || dataMovement2 == 1) { agvStatus = "  PAUSE"; agv2Status = "PAUSE"; }
                        else if (dataMovement == 2 || dataMovement2 == 1) { agvStatus = "  RUN"; agv2Status = "RUN"; }
                        else { }

                        if (dataRute == 1 || dataRute == 2 || dataRute == 3 || dataRute == 4 || dataRute == 5)
                        {
                            agvRoute = "GO TO LINE";
                            if (dataRfid == 11) { labelPosition.Text = agvRoute; }
                            else { labelPosition.Text = arrayPosition[indexRFID]; }
                        }
                        // Button Route 1
                        else if ((dataRute == 1 || dataRute == 2 || dataRute == 3 || dataRute == 4 || dataRute == 5) && dataRfid == 1)
                        {
                            wip1.Visible = false; wip2.Visible = false; wip3.Visible = false; wip4.Visible = false; wip5.Visible = false; wip6.Visible = false;
                        }
                        // Button Route 2
                        else if (dataRute == 20 && (dataRfid == 32 || dataRfid == 31 || dataRfid == 1 || dataRfid == 2))
                        {
                            agvRoute = "GO TO WIP";
                            labelPosition.Text = agvRoute;
                        }
                        // Transfer WIP FUll 1
                        else if (dataRute == 20 && dataRfid == 10)
                        {
                            wipFull1.Visible = true;
                        }
                        else if (dataRute == 20 || dataRute == 30 )
                        {
                            //agvRoute = "GO HOME";
                            if ((dataRfid == 5 || labelPosition.Text == "HOME") && statusDelivery == "FINISH" )
                            {
                                activeLine(false, false, false, false, false, false);
                                labelPosition.Text = arrayPosition[indexRFID];
                                agvHome1.Visible = true; 
                                //agvLabel1.Visible = true;
                                agvIcon.Visible = false; agvFlip1.Visible = false; agvFlip2.Visible = false;
                                Console.WriteLine("cok");
                            }
                            else if (dataRfid == 9) { agvIcon.Visible = false;}
                            wipFull1.Visible = false;
                            wipFull2.Visible = false;
                        }
                        else
                        {
                            agvRoute = "STANDBY";
                            labelPosition.Text = arrayPosition[indexRFID];
                            agvHome1.Visible = true;
                            //agvLabel1.Visible = false;
                            agvIcon.Visible = false;
                        }

                        // Visualization
                        if (dataRfid == 1 && dataRute != 20)
                        {
                            agvFlip1.Visible = true; agvFlip2.Visible = false; agvIcon.Visible = false;
                            agvFlip1.Left = arrayRfidLoc_X[indexRFID]; agvFlip1.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        else if (dataRfid == 11 && dataRute != 20)
                        {
                            agvIcon.Visible = false;
                        }
                        else if (dataRfid == 31 && dataRute != 20)
                        {
                            agvFlip1.Visible = false; agvFlip2.Visible = true; agvIcon.Visible = false;
                            agvFlip2.Left = arrayRfidLoc_X[indexRFID]; agvFlip2.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        else if (dataRfid == 32 && dataRute != 20)
                        {
                            agvFlip2.Visible = false; agvIcon.Visible = true;
                            agvIcon.Left = arrayRfidLoc_X[indexRFID]; agvIcon.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        else if (dataRfid == 32) 
                        {
                            agvIcon.Visible = true;
                            agvIcon.Left = arrayRfidLoc_X[indexRFID]; agvIcon.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        else if (dataRfid == 31 && dataRute == 20)
                        {
                            moveAnimation.Start();
                            agvIcon.Visible = false;
                            agvFlip2.Visible = false;
                            agvFlip3.Visible = false;
                            agvFlip4.Visible = true;
                            int position = rfidVirtual1.Location.X + moveCnt;
                            agvFlip4.Left = position;
                            agvFlip4.Top = rfidVirtual1.Location.Y;
                            Console.WriteLine("moveCnt : {0}, agvPost : {1} X : {2} ", moveCnt, position, rfidVirtual2.Location.X);
                            if (agvFlip4.Location.X > rfidVirtual2.Left || dataRfid == 11)
                            {
                                moveAnimation.Stop();
                                agvFlip4.Visible = false;
                                agvFlip3.Visible = true;
                                agvFlip3.Left = rfidVirtual2.Location.X;
                                agvFlip3.Top = rfidVirtual2.Location.Y;
                            }
                            else { agvFlip4.Visible = true; agvFlip3.Visible = false; }
                        }
                        else if (dataRfid == 11 && dataRute == 20)
                        {
                            agvFlip4.Visible = false;
                            agvFlip2.Visible = false;
                            agvFlip3.Visible= true;
                            agvIcon.Visible = false;
                            agvFlip3.Left = arrayRfidLoc_X[indexRFID];
                            agvFlip3.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        else if (dataRfid == 1 && dataRute == 20)
                        {
                            agvFlip1.Visible = true;
                            agvFlip2.Visible = false;
                            agvFlip3.Visible = false;
                            agvIcon.Visible = false;
                            agvFlip1.Left = arrayRfidLoc_X[indexRFID];
                            agvFlip1.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        else if (dataRfid == 2 && dataRute == 20)
                        {
                            agvFlip1.Visible = false;
                            agvFlip2.Visible = false;
                            agvFlip3.Visible = false;
                            agvFlip4.Visible = false;
                            agvIcon.Visible = true;
                            agvIcon.Left = arrayRfidLoc_X[indexRFID];
                            agvIcon.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        else if (dataRfid == 5)
                        {
                            agvHome1.Visible = true;
                            //agvLabel1.Visible = true;
                        }
                        else if (dataRute == 1)
                        {
                            Console.WriteLine("Bypass Rute 1");
                            labelPosition.Text = arrayPosition[indexRFID];
                            agvIcon.Visible = true;
                            agvFlip3.Visible = false;
                            agvFlip4.Visible = false;
                            agvHome1.Visible = false;
                            //agvLabel1.Visible = false;
                            agvIcon.Left = arrayRfidLoc_X[indexRFID];
                            agvIcon.Top = arrayRfidLoc_Y[indexRFID];
                            
                        }
                        else if (dataRfid ==0)
                        {
                            //Console.WriteLine("RFID = 0");
                            agvIcon.Visible = false;
                            agvHome1.Visible = false;
                            //agvLabel1.Visible = false;
                        }
                        else 
                        {
                            labelPosition.Text = arrayPosition[indexRFID];
                            agvIcon.Visible = true;
                            agvIcon.Left = arrayRfidLoc_X[indexRFID];
                            agvIcon.Top = arrayRfidLoc_Y[indexRFID];
                        }
                        disini("", "", "492");
                        AGVStatusModel temp = new AGVStatusModel("  "+agvName, agvState, agvStatus.ToString());
                        AGVStatusModel temp2 = new AGVStatusModel(agv2Name, agv2State, agv2Status.ToString());
                        showData.Add(temp);
                        showData.Add(temp2);
                    }
                    // PRINT ALL
                    //Console.WriteLine("\ndataRFID : {0} dataRute : {1} LastPost : {2}  \ndataMovement : {3} \nagvicon.X : {4} \nagvicon.Y : {5} \nstatusDelivery : {6} offTime : {7}"
                    //                    , dataRfid, dataRute, arrayPosition[indexRFID], dataMovement, agvIcon.Left, agvIcon.Top, statusDelivery, offTime);
                }
                //disini("669", "showdata", "");
                gridViewStatus.Invoke((MethodInvoker)delegate { gridViewStatus.DataSource = showData; });

            }
            else
            {
                Console.WriteLine("Else cok");
                List<AGVStatusModel> showData = new List<AGVStatusModel>();
                AGVStatusModel temp = new AGVStatusModel(agvName, agvState, agvStatus.ToString());
                showData.Add(temp);
                gridViewStatus.Invoke((MethodInvoker)delegate { gridViewStatus.DataSource = showData; });
            }

            //==================================================================================================================================// --> API3
            data = await API("devC.getDeviceList()");
            if (data.errMark == "OK")
            {
                double agvAddress,agvAddress2;
                string on1 = "   ON-1", on2 = "ON-2", off1 = "OFF-1", off2 = "OFF-2";
                List<AGVDeviceModel> showDevice = new List<AGVDeviceModel>();
                List<AGV2DeviceModel> showDevice2 = new List<AGV2DeviceModel>();
                for (int i = 0; i < data.msg.Count; i++)
                {
                    agvAddress = data.msg[0][3]; agvAddress2 = data.msg[1][3];
                    offTime = data.msg[0][6];
                    if ((agvAddress == 1) && readType == "车" && offTime >= 3)
                    {
                        agvState = off1;
                        string disc = agvName + " DISCONNECTED";
                        Console.WriteLine(disc);
                        labelDisconnect.Text = disc;
                        labelDisconnect.Visible = true;
                    }
                    else if (agvAddress == 1 && readType == "车" && offTime < 0.3)
                    {
                        agvState = on1;
                        labelDisconnect.Visible = false;
                    }
                    if ((agvAddress2 == 2) && readType == "车" && offTime >= 3)
                    {
                        agv2State = off2;
                        string disc = agv2Name + " DISCONNECTED";
                        Console.WriteLine(disc);
                        labelDisconnect.Text = disc;
                        labelDisconnect.Visible = true;
                    }
                    else if (agvAddress2 == 2 && readType == "车" && offTime < 0.3)
                    {
                        agv2State = on2;
                        labelDisconnect.Visible = false;
                    }
                    else { //Console.WriteLine("API 3 Else"); 
                    }
                    
                }
                
                if (agvState == "ON-1")               // Update Obstacle & Emergency STOP
                {
                    //==================================================================================================================================// --> API4
                    ResponseData2 datanonArray = await APInonArray("devC.deviceDic[1].optionsLoader.load(carLib.RAM.DEV.BTN_EMC)");
                    if (datanonArray.errMark == "OK")
                    {
                        string errorCode = "";
                        List<AGVErrorModel> showError = new List<AGVErrorModel>();
                        for (int j = 0; j < datanonArray.msg.Count; j++)
                        {
                            long btnState = datanonArray.msg[1];
                            if (btnState == 0) { errorCode = "EMC STOP"; }
                            else { errorCode = "-"; }
                            AGVErrorModel temp = new AGVErrorModel(DateTime.Now.ToString(), agvName, errorCode, obsCode);
                            showError.Add(temp);
                            gridViewError.Invoke((MethodInvoker)delegate { gridViewError.DataSource = showError; });
                        }
                    }
                    else
                    {
                        List<AGVErrorModel> showError = new List<AGVErrorModel>();
                        AGVErrorModel temp = new AGVErrorModel("", agvName, "", obsCode);
                        showError.Add(temp);
                        gridViewError.Invoke((MethodInvoker)delegate { gridViewError.DataSource = showError; });
                    }
                    //==================================================================================================================================// --> API5
                    datanonArray = await APInonArray("devC.deviceDic[1].optionsLoader.load(carLib.RAM.DEV.OBS)");
                    if (datanonArray.errMark == "OK")
                    {
                        List<AGVErrorModel> showError = new List<AGVErrorModel>();
                        for (int k = 0; k < datanonArray.msg.Count; k++)
                        {
                            long btnState = datanonArray.msg[2];
                            if (btnState != 0)
                            {
                                obsCode = "OBS STOP";
                            }
                            else { obsCode = "-"; }
                        }
                    }
                    this.gridViewError.Columns[0].Width = 90;
                    updateError = 0;
                }
                else if (agv2State == "ON-2")
                {
                    Console.WriteLine("2");
                }

            }
            else if (data.errMark=="err") { 
                agvState = "OFF"; 
                string disc = agvName + " DISCONNECTED";
                Console.WriteLine(disc);
                labelDisconnect.Text = disc;
                labelDisconnect.Visible = true;
            }
            callAPI();
        }
        
        private void timer5_Tick(object sender, EventArgs e)
        {
            waitingTime += 1;
        }

        [Obsolete]
        public Form1()
        {
            //this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            InitializeComponent();
            gridViewStatus.Invoke((MethodInvoker)delegate { gridViewStatus.DataSource = AGVStatus; });
            gridViewDS.Invoke((MethodInvoker)delegate { gridViewDS.DataSource = AGVData; });
            gridViewError.Invoke((MethodInvoker)delegate { gridViewError.DataSource = AGVError; });
            AutoClosingMessageBox.Show("Connecting to The server...","SYSTEM INFO", 10000);
            callAPI();
        }

        //=============================================================Backend Service=============================================================//
        private async Task<ResponseData> API(string command)
        {
            var cmd = new RequestData();
            cmd.command = command;
            cmd.serialNumber = 0;
            var json = JsonConvert.SerializeObject(cmd);
            //Console.WriteLine(json);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            var response = await client.PostAsync(url, data);
            ResponseData ret;
            //Console.Write("Response Data : {0}", data);
            while (true)
            {
                ret = JsonConvert.DeserializeObject<ResponseData>(response.Content.ReadAsStringAsync().Result);
                if (ret.command == cmd.command)
                {
                    break;
                }
            }
            return ret;
            //Console.WriteLine("Error Message: {0}", ret.errMark);
            //Console.WriteLine("Message: {0}", ret.msg);
            //Console.WriteLine("IP Robot: {0}", ret.msg[0][5][0]);
            //Console.WriteLine(ret);
            //string result = response.Content.ReadAsStringAsync().Result;
            ////Console.WriteLine(result);
        }
        private async Task<ResponseData2> APInonArray(string command)
        {
            var cmd = new RequestData();
            cmd.command = command;
            cmd.serialNumber = 0;

            var json = JsonConvert.SerializeObject(cmd);
            //Console.WriteLine(json);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var client = new HttpClient();
            var response = await client.PostAsync(url, data);
            ResponseData2 ret;
            //Console.Write("Response Data : {0}", data);
            while (true)
            {
                ret = JsonConvert.DeserializeObject<ResponseData2>(response.Content.ReadAsStringAsync().Result);
                if (ret.command == cmd.command)
                {
                    break;
                }
            }
            return ret;
            
        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = e.Location.X - _mouseLoc.X;
                int dy = e.Location.Y - _mouseLoc.Y;
                this.Location = new System.Drawing.Point(this.Location.X + dx, this.Location.Y + dy);
            }
        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseLoc = e.Location;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            dateLabel.Text = DateTime.Now.ToString();
        }
        private void timer2_Tick(object sender, EventArgs e)
        {
            updateError += 1;
        }
        private void timer3_Tick(object sender, EventArgs e)
        {
            moveCnt += 1;

        }
        private void timer4_Tick(object sender, EventArgs e)
        {

        }
        private void detailError_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Text = "Detail Error History";
            form2.HelpButton = true;
            form2.FormBorderStyle = FormBorderStyle.FixedDialog;
            form2.StartPosition = FormStartPosition.CenterScreen;
            form2.ShowDialog();


        }
        private void detailDelivery_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.Text = "Detail Delivery Status";
            form3.HelpButton = true;
            form3.FormBorderStyle = FormBorderStyle.FixedDialog;
            form3.StartPosition = FormStartPosition.CenterScreen;
            form3.ShowDialog();
            Console.Write("writelog");

        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;

        }
        public static void writeLog(string logTime, string logName, long logJobid, string logStatusDlv)
        {
            string logData = logTime + "," + logName + "," + logJobid + "," + logStatusDlv;
            try
            {
                File.AppendAllText(@"D:\WORK\SAMSUNG\SampleUI-SamsungAGV\log.txt", logData.ToString() + Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.WriteLine("error catch : {0}", e.Message);
            }
        }
        private void closeButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        private void batteryLevel1_ProgressChanged(object sender, Bunifu.UI.WinForms.BunifuProgressBar.ProgressChangedEventArgs e)
        {

        }

        private void batteryLevel2_ProgressChanged(object sender, Bunifu.UI.WinForms.BunifuProgressBar.ProgressChangedEventArgs e)
        {

        }

        public static void disini(string textInput,string i2,string i3)
        {
            Console.WriteLine("Lagi Disini : {0}, {1}, {2}", textInput,i2,i3);
        }
    }
}

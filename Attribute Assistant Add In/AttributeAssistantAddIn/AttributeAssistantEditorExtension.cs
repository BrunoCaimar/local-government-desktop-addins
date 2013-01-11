﻿/*
 | Version 10.1.1
 | Copyright 2012 Esri
 |
 | Licensed under the Apache License, Version 2.0 (the "License");
 | you may not use this file except in compliance with the License.
 | You may obtain a copy of the License at
 |
 |    http://www.apache.org/licenses/LICENSE-2.0
 |
 | Unless required by applicable law or agreed to in writing, software
 | distributed under the License is distributed on an "AS IS" BASIS,
 | WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 | See the License for the specific language governing permissions and
 | limitations under the License.
 */


using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Data;
using System.Windows.Forms;
using System.Net;
using System.Web;
using System.Xml;
//using System.Runtime.Serialization.JSON;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

using ESRI.ArcGIS.CartoUI;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.ArcMap;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Location;
using System.Drawing;
using System.Reflection;

using A4LGSharedFunctions;



namespace ArcGIS4LocalGovernment
{

    public class LastValueEntry
    {
        public LastValueEntry(object value, bool on_Create, bool on_changeAtt, bool on_ChangeGeo, bool on_Manual)
        {
            Value = value;
            On_Create = on_Create;
            On_ChangeAtt = on_changeAtt;
            On_ChangeGeo = on_ChangeGeo;
            On_Manual = on_Manual;
        }
        public LastValueEntry()
        {
            Value = null;
            On_Create = false;
            On_ChangeAtt = false;
            On_ChangeGeo = false;
            On_Manual = false;
        }

        public object Value { get; set; }
        public bool On_Create { get; set; }
        public bool On_ChangeAtt { get; set; }
        public bool On_ChangeGeo { get; set; }
        public bool On_Manual { get; set; }
    }
    public static class AAState
    {
        public enum intersectOptions { Centroid, PromptMulti, First }
        public static ESRI.ArcGIS.esriSystem.IPropertySet2 lastValueProperties;
        public static string _filePath = "";

        public static ISpatialReference _sr1;
        public static RotationCalculator rCalc;
        public static int _processCount = 0;
        public static IEditEvents_Event _editEvents;

        public static ISpatialReferenceFactory spatRefFact = new SpatialReferenceEnvironmentClass();
        public static IGeographicCoordinateSystem srWGS84 = spatRefFact.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
        public static CurrentUserInfo _currentUserInfo;
        private static string _Debug = "false";
        public static string Debug()
        {
            return ConfigUtil.GetConfigValue("AttributeAssistant_Debug", AAState._Debug);

        }
        private static bool _PerformUpdates = true;
        public static bool PerformUpdates
        {
            get
            {
                return _PerformUpdates;
            }

            set
            {


                _PerformUpdates = value;

                AAState.setIcon();
            }
        }
        public static bool _Suspend = false;

        public static Bitmap bmpOff;
        public static Bitmap bmpOn;
        public static ICommandItem commandItem;
        public static ITable _gentab;
        public static ITable _tab;
        public static DataTable _dt;
        public static DataView _dv;
        public static IEditor _editor;
        public static StreamWriter _sw;
        public static string indent = "";


        public static bool _CheckEnvelope = false;

        // Declare configuration variables 
        public static string _defaultsTableName = "DynamicValue";
        public static string _sequenceTableName = "GenerateId";

        public static void setIcon()
        {
            try
            {
                if (bmpOn == null)
                {
                    LoadBitmaps();
                }
                LoadCommand();
                if (commandItem == null)
                    return;

                if (AAState.PerformUpdates && !commandItem.Caption.ToString().Contains("on"))
                {


                    commandItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(bmpOn);
                    commandItem.Caption = "Attribute Assistant is on";
                    AAState.WriteLine("Attribute Assistant is being turn on");

                }
                else if (AAState.PerformUpdates == false && !commandItem.Caption.ToString().Contains("off"))
                {

                    commandItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(bmpOff);
                    commandItem.Caption = "Attribute Assistant is off";
                    AAState.WriteLine("Attribute Assistant is being turn off");

                }
                else if (commandItem.Caption.ToString().Contains("startup"))
                {

                    if (AAState.PerformUpdates)
                    {


                        commandItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(bmpOn);
                        commandItem.Caption = "Attribute Assistant is on";
                        AAState.WriteLine("Attribute Assistant is being turn on");

                    }
                    else if (AAState.PerformUpdates == false)
                    {

                        commandItem.FaceID = ESRI.ArcGIS.ADF.COMSupport.OLE.GetIPictureDispFromBitmap(bmpOff);
                        commandItem.Caption = "Attribute Assistant is off";
                        AAState.WriteLine("Attribute Assistant is being turn off");

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting AA Toggle button: " + ex.Message);

            }

        }
        public static void LoadCommand()
        {
            try
            {
                AAState.commandItem = ArcMap.Application.Document.CommandBars.Find("ArcGIS4LocalGovernment_AttributeAssistantToggleCommand", true, false);
            }
            catch
            {
            }
        }
        public static void LoadBitmaps()
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream myStream = myAssembly.GetManifestResourceStream("ArcGIS4LocalGovernment.Images.AttributeAssistantToggleOnSolidOff.png");
            AAState.bmpOff = new Bitmap(myStream);
            AAState.bmpOff.MakeTransparent(AAState.bmpOff.GetPixel(0, 0));

            Stream myStream2 = myAssembly.GetManifestResourceStream("ArcGIS4LocalGovernment.Images.AttributeAssistantToggleOnSolidOn.png");

            AAState.bmpOn = new Bitmap(myStream2);
            AAState.bmpOn.MakeTransparent(AAState.bmpOn.GetPixel(0, 0));




        }
        public static bool initTable()
        {
            if (_editor == null)
            {
                _editor = ArcMap.Application.FindExtensionByName("esriEditor.Editor") as IEditor;
            }
            if (_editor == null)
            {

                return false;
            }
            // Disable editor extension if editing shapefiles, coverages, etc.
            initDynTable();



            if (_dt == null)
            {

                return false;
            }
            _dv = new DataView(_dt);
            _gentab = Globals.FindTable(ArcMap.Document.FocusMap, _sequenceTableName);
            if (_gentab == null)
            {

            }



            return true;
        }
        public static void initDynTable()
        {
            _dt = getConfigDataTable();


        }
        private static DataTable getConfigDataTable()
        {
            try
            {
                DataTable dt = new DataTable();

                _tab = Globals.FindTable(ArcMap.Document.FocusMap, _defaultsTableName);
                if (_tab == null)
                    return null;

                for (int i = 0; i < _tab.Fields.FieldCount; i++)
                {
                    if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeString)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeGlobalID)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeGUID)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else if (_tab.Fields.get_Field(i).Type == esriFieldType.esriFieldTypeDate)
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(string));
                    else
                        dt.Columns.Add(_tab.Fields.get_Field(i).Name, typeof(int));
                }

                ICursor cur = _tab.Search(null, true);
                IRow row;
                System.Collections.ArrayList ar = new System.Collections.ArrayList();

                while ((row = cur.NextRow()) != null)
                {
                    for (int i = 0; i < _tab.Fields.FieldCount; i++)
                    {
                        ar.Add(row.get_Value(i));
                    }
                    dt.Rows.Add(ar.ToArray());
                    ar.Clear();
                }
                if (row != null)
                {
                    Marshal.ReleaseComObject(row);

                    GC.WaitForFullGCComplete();
                    row = null;
                }
                if (cur != null)
                {
                    Marshal.ReleaseComObject(cur);
                    GC.Collect(300);
                    GC.WaitForFullGCComplete();
                    cur = null;
                }

                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetConfigDataTable: " + ex.Message);
                return null;
            }
        }
        public static void WriteLine(string valToWrite)
        {
            try
            {
                if (AAState._sw == null)
                    return;
                else
                    AAState._sw.WriteLine(AAState.indent + valToWrite);
            }
            catch (Exception ex)
            {
                MessageBox.Show("WriteLine: " + ex.Message);
            }

        }
        public static void createLastValueProperrtySet()
        {
            try
            {

                object nullObject = null;
                if (AAState.lastValueProperties == null || _clearLastValue == true)
                {
                    AAState.lastValueProperties = new PropertySetClass();
                }


                DataView dv = new DataView(AAState._dt);
                dv.RowFilter = "ValueMethod = 'LAST_VALUE'";
                string[] args;

                foreach (DataRowView drv in dv)
                {
                    if (drv["FIELDNAME"].ToString() == "SHAPE")
                    {
                        MessageBox.Show("You have specified SHAPE in a last value entry in the Attribute Assistant, this is not supported");

                    }
                    else
                    {
                        if (_clearLastValue)
                        {
                            LastValueEntry lstV = new LastValueEntry();
                            lstV.Value = nullObject;
                            lstV.On_ChangeAtt = Globals.toBoolean(drv["ON_CHANGE"].ToString());

                            if (drv.DataView.Table.Columns["ON_CHANGEGEO"] != null)
                            {
                                lstV.On_ChangeGeo = Globals.toBoolean(drv["ON_CHANGEGEO"].ToString());
                            }
                            lstV.On_Create = Globals.toBoolean(drv["ON_CREATE"].ToString());
                            lstV.On_Manual = Globals.toBoolean(drv["ON_MANUAL"].ToString());

                            AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstV);
                            lstV = null;

                        }
                        else
                        {
                            string valData = drv["VALUEINFO"].ToString().Trim();
                            if (valData.Contains(Environment.NewLine))
                            {
                                valData = valData.Substring(0, valData.IndexOf(Environment.NewLine));


                            }
                            if (valData.Trim() == "")
                            {
                                nullObject = null;
                            }
                            else
                            {
                                args = valData.Split('|');
                                if (args.Length == 2)
                                {
                                    nullObject = args[1] as System.Object;

                                }
                            }
                            try
                            {
                                object temp = AAState.lastValueProperties.GetProperty(drv["FIELDNAME"].ToString());
                                if (nullObject != null && temp == null)
                                {
                                    LastValueEntry lstV = new LastValueEntry();
                                    lstV.Value = nullObject;
                                    lstV.On_ChangeAtt = Globals.toBoolean(drv["ON_CHANGE"].ToString());
                                    if (drv.DataView.Table.Columns["ON_CHANGEGEO"] != null)
                                    {
                                        lstV.On_ChangeGeo = Globals.toBoolean(drv["ON_CHANGEGEO"].ToString());
                                    }
                                    lstV.On_Create = Globals.toBoolean(drv["ON_CREATE"].ToString());
                                    lstV.On_Manual = Globals.toBoolean(drv["ON_MANUAL"].ToString());

                                    AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstV);
                                    lstV = null;

                                }
                            }
                            catch
                            {

                                LastValueEntry lstV = new LastValueEntry();
                                lstV.Value = nullObject;
                                lstV.On_ChangeAtt = Globals.toBoolean(drv["ON_CHANGE"].ToString());
                                if (drv.DataView.Table.Columns["ON_CHANGEGEO"] != null)
                                {
                                    lstV.On_ChangeGeo = Globals.toBoolean(drv["ON_CHANGEGEO"].ToString());
                                }
                                lstV.On_Create = Globals.toBoolean(drv["ON_CREATE"].ToString());
                                lstV.On_Manual = Globals.toBoolean(drv["ON_MANUAL"].ToString());

                                AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstV);
                                lstV = null;
                            }


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("createLastValueProperrtySet: " + ex.Message);
            }
        }
        public static void promptLastValueProperrtySetOneForm()
        {
            try
            {

                object nullObject = null;
                if (AAState.lastValueProperties == null)
                {
                    MessageBox.Show("The last value array has not been created, please activate the extension");
                    return;
                }


                object lstNames;
                object lstValues;

                AAState.lastValueProperties.GetAllProperties(out lstNames, out lstValues);

                LastValueEntry lstVal;

                DataTable pDt = new DataTable("LastValues");

                DataColumn pDC = pDt.Columns.Add("Field", typeof(String));
                pDC.ReadOnly = true;
                pDC.Caption = "Last Value Field";

                pDC = pDt.Columns.Add("Value", typeof(String));
                pDC.ReadOnly = false;
                pDC.Caption = "Current Value";

                pDC = pDt.Columns.Add("Changed", typeof(String));
                pDC.ReadOnly = false;
                pDC.Caption = "Change";


                DataRow pDR;
                for (int i = 0; i < AAState.lastValueProperties.Count; i++)
                {

                    pDR = pDt.NewRow();

                    string strVal = ((object[])lstNames)[i].ToString();
                    pDR["Field"] = strVal;

                    pDR["Changed"] = "F";
                    lstVal = AAState.lastValueProperties.GetProperty(strVal) as LastValueEntry;
                    if (lstVal != null)
                    {
                        if (lstVal.Value == null || lstVal.Value == DBNull.Value)
                        {
                            nullObject = "<null>";
                        }
                        else
                        {
                            nullObject = lstVal.Value.ToString();
                        }
                        pDR["Value"] = nullObject.ToString();

                    }
                    pDt.Rows.Add(pDR);



                }
                LastValueForm lstValF = new LastValueForm();
                lstValF.setDataTable(pDt);
                DialogResult dres = lstValF.ShowDialog();
                if (dres == DialogResult.OK)
                {
                    foreach (DataRow dr in lstValF.getDataTable().Rows)
                    {
                        if (dr[2].ToString() == "T")
                        {
                            lstVal = AAState.lastValueProperties.GetProperty(dr[0].ToString()) as LastValueEntry;
                            if (lstVal != null)
                            {
                                lstVal.Value = dr[1].ToString();
                                AAState.lastValueProperties.SetProperty(dr[0].ToString(), lstVal);
                            }
                        }
                    }
                }



            }
            catch (Exception ex)
            {
                MessageBox.Show("promptLastValueProperrtySet: " + ex.Message);
            }
        }
        public static void promptLastValueProperrtySet()
        {
            try
            {

                object nullObject = null;
                if (AAState.lastValueProperties == null)
                {
                    MessageBox.Show("The last value array has not been created, please activate the extension");
                    return;
                }


                object lstNames;
                object lstValues;

                AAState.lastValueProperties.GetAllProperties(out lstNames, out lstValues);
                LastValueEntry lstVal;
                for (int i = 0; i < AAState.lastValueProperties.Count; i++)
                {
                    string strVal = ((object[])lstNames)[i].ToString();

                    lstVal = AAState.lastValueProperties.GetProperty(strVal) as LastValueEntry;
                    if (lstVal != null)
                    {
                        if (lstVal.Value == null || lstVal.Value == DBNull.Value)
                        {
                            nullObject = "<null>";
                        }
                        else
                        {
                            nullObject = lstVal.Value.ToString();
                        }
                        string strRet = Microsoft.VisualBasic.Interaction.InputBox("Set Property for " + strVal + "\r\nCurrent Value is: " + nullObject.ToString() + "\r\nUse <null> for a null value", "Set Property for " + strVal, nullObject.ToString());

                        if (strRet != "")
                        {
                            if (strRet.ToString().ToUpper() == "<null>".ToUpper())
                            {
                                lstVal.Value = null;

                                AAState.lastValueProperties.SetProperty(strVal, lstVal);
                            }
                            else
                            {
                                lstVal.Value = strRet;

                                AAState.lastValueProperties.SetProperty(strVal, lstVal);
                            }
                        }
                    }



                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("promptLastValueProperrtySet: " + ex.Message);
            }
        }
        public static void initEditing()
        {
            if (AAState.PerformUpdates == false)
            {
                AAState.WriteLine("Attribute Assistant is off - InitEditing1");
                return;
            }
            // wire events
            if (reInitExt() == false)
            {
                AAState.WriteLine("Attribute Assistant is being turn off - InitEditing2");
                AAState.PerformUpdates = false;

            }
            if (AAState._editor.EditWorkspace != null)
            {
                if (AAState._editor.EditWorkspace.Type == esriWorkspaceType.esriFileSystemWorkspace)
                {
                    AAState.WriteLine("Attribute Assistant is being turn off - InitEditing FileWorkspace");
                    AAState.PerformUpdates = false;

                }
            }
            if (AAState.PerformUpdates)
            {
                AAState.WriteLine("Attribute Assistant is on - InitEditing");
                if (Debug().ToUpper() == "TRUE")
                {
                    AAState._filePath = Globals.PromptForSave();
                    if (AAState._filePath != "")
                    {
                        AAState._sw = Globals.createTextFile(AAState._filePath, FileMode.Create);


                    }
                    Globals.LogLocations = AAState._filePath;
                }
                else
                {
                    AAState._sw = null;
                    Globals.LogLocations = "";
                }


                // Get user Info


                reInitExt();


                //obtain rotation calculator
                AAState.rCalc = new RotationCalculator(ArcMap.Application);

                //Create WGS_84 SR
                ISpatialReferenceFactory srFactory = new SpatialReferenceEnvironmentClass();
                IGeographicCoordinateSystem gcs = srFactory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
                _sr1 = gcs;




                _editEvents.OnChangeFeature += FeatureChange;
                _editEvents.OnCreateFeature += FeatureCreate;

            }
            else
            {

                AAState.PerformUpdates = false;

                _editEvents.OnChangeFeature -= FeatureChange;
                _editEvents.OnCreateFeature -= FeatureCreate;




            }

            ArcMap.Application.StatusBar.set_Message(0, "Editor Extension Initialized");
        }
        public static void StopChangeMonitor()
        {
            _editEvents.OnChangeFeature -= FeatureChange;
        }
        public static void StartChangeMonitor()
        {
            _editEvents.OnChangeFeature += FeatureChange;
        }
        public static bool reInitExt()
        {
            AAState.indent = "";
            bool blState = AAState.initTable();
            AAState.createLastValueProperrtySet();
            _currentUserInfo = new CurrentUserInfo(AAState._editor);
            return blState;


        }
        public static void unInitEditing()
        {
            try
            {
                if (AAState._sw != null)
                {
                    AAState.WriteLine("**************************************");
                    AAState.WriteLine("Closing Log File " + DateTime.Now.ToString());
                    AAState._sw.Flush();
                    AAState._sw.Close();
                    AAState._sw = null;

                }
                AAState._filePath = "";
                // Globals.LogLocations = AAState._filePath;
            }
            catch { }
            try
            {




                _editEvents.OnChangeFeature -= FeatureChange;
                _editEvents.OnCreateFeature -= FeatureCreate;

                _currentUserInfo = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on the Stop Editing: " + ex.Message);

            }
        }

        public static event OnChangeFeature changeFeature;
        public delegate void OnChangeFeature(ESRI.ArcGIS.Geodatabase.IObject obj);
        public static event OnChangeGeoFeature changeGeoFeature;
        public delegate void OnChangeGeoFeature(ESRI.ArcGIS.Geodatabase.IObject obj);
        public static event OnCreateFeature createFeature;
        public delegate void OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj);

        public static event OnManualFeature manualFeature;
        public delegate void OnManualFeature(ESRI.ArcGIS.Geodatabase.IObject obj);
        public static void FeatureChange(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {

                if (changeFeature != null)
                    changeFeature(obj);


            }
            catch (Exception ex)
            {
                MessageBox.Show("OnChangeFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);

            }
        }
        public static void FeatureGeoChange(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {

                if (changeGeoFeature != null)
                    changeGeoFeature(obj);


            }
            catch (Exception ex)
            {
                MessageBox.Show("OnChangeFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);

            }
        }
        public static void FeatureCreate(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {

                if (createFeature != null)
                    createFeature(obj);


            }
            catch (Exception ex)
            {
                MessageBox.Show("OnCreateFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);
            }
        }
        public static void FeatureManual(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {

                if (manualFeature != null)
                    manualFeature(obj);


            }
            catch (Exception ex)
            {
                MessageBox.Show("OnFeatureManual:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);

            }
        }
        public static bool _clearLastValue = false;

    }

    public class AttributeAssistantEditorExtension : ESRI.ArcGIS.Desktop.AddIns.Extension
    {



        IFeature inFeature;

        static int _LastOID = -1;
        static string _LastMode = "";
        static string _LastFC = "";

        //The schema of the dynamic values table is fixed.  Please copy the AttributeAssistant table provided in the sample data.  
        //If it is modiifed these field positions may be incorrect.


        private string lastEditorName;


        private bool changed;
        private string tableName, valMethod, valData, valFC;

        private MSScriptControl.ScriptControlClass script;
        private LastValueEntry lastValue;
        private int fieldCopy, juncField;
        private Nullable<double> rotationAngle;

        private string ConcatDelim = ",";

        private string[] args;
        private string[] sourceLayerNames;

        private string sequenceColumnName, sequenceFixedWidth, sequencePostfix = "";

        private string formatString;
        private string intersectLayerName, intersectLayerFieldName;
        private int sequenceColumnNum;
        private int sequencePadding;
        private long sequenceValue;
        private bool found;

        private string newValue;
        private string sourceLayerName;
        private string sourceFieldName;
        private double searchDistance, distance, lastDistance;
        private int sourceField;
        private string[] _currentDatasetNameItems;


        private bool _enabledOnStart = true;

        private IFeatureLayer intersectLayer;
        private bool boolLayerOrFC = false;
        private IStandaloneTable intersectTable;
        private IFeatureSelection intersectLayerSelection;
        private ITableSelection intersectTableSelection;
        private IQueryFilter qFilter;
        private IRow row;
        private IField testField;

        private ICurve curve;
        private IFeatureLayer sourceLayer;
        private IFeatureSelection pFS;

        private IPoint _copyPoint;
        private IPolyline _copyPolyline;
        private IPolygon _copyPolygon;
        private ISpatialFilter sFilter;
        private IFeatureCursor fCursor;
        private ICursor cCurs = null;

        private IFeature sourceFeature;
        private IFeature nearestFeature;
        private IField fieldObj;
        private IDataset _currentDataset;
        private IProximityOperator proxOp;

        private int intersectFieldPos;

        private string intersectValue;

        string locatorURL;
        string GeocodeStr = "GeocodeServer";
        string reverseGeocodeStr = "ReverseGeocode";

        private string _agsOnlineLocators = "http://tasks.arcgisonline.com/ArcGIS/rest/services/Locators/TA_Streets_US_10/GeocodeServer/";

        //Retrieve configuration from XML config file (overrides defaults set above)
        private void GetConfigSettings()
        {

            AAState._defaultsTableName = ConfigUtil.GetConfigValue("AttributeAssistant_TableName", AAState._defaultsTableName);
            AAState._sequenceTableName = ConfigUtil.GetConfigValue("AttributeAssistant_GenerateId_TableName", AAState._sequenceTableName);
            AAState._CheckEnvelope = ConfigUtil.GetConfigValue("AttributeAssistant_CheckEnvelope ", AAState._CheckEnvelope);
            _enabledOnStart = ConfigUtil.GetConfigValue("AttributeAssistant_EnabledOnStartUp", _enabledOnStart);
            _agsOnlineLocators = ConfigUtil.GetConfigValue("Geocoder", _agsOnlineLocators);
            if (_agsOnlineLocators[_agsOnlineLocators.Length - 1] == '/')
            {
                _agsOnlineLocators = _agsOnlineLocators.Substring(0, _agsOnlineLocators.Length - 1);
            }
            if (ConfigUtil.GetConfigValue("AttributeAssistant_ClearLastValue", _agsOnlineLocators).ToUpper() == "TRUE")
                AAState._clearLastValue = true;
            else
                AAState._clearLastValue = false;



        }


        public AttributeAssistantEditorExtension()
        {


        }
        void appStatusEvents_Initialized()
        {

            if (localEventAdded == false)
            {
                init();
            }


        }
        private bool localEventAdded = false;
        void init()
        {

            AAState.initDynTable();
            if (!_enabledOnStart || AAState._dt == null)
            {

                AAState.PerformUpdates = false;
                AAState.WriteLine("Attribute Assistant is turn off - init");

            }
            else
            {
                AAState.WriteLine("Attribute Assistant is turn on - init");
                AAState.PerformUpdates = true;

            }

            AAState.initEditing();

            if (localEventAdded == false)
            {
                AAState.WriteLine("Attribute Assistant Adding Event Handlers");
                AAState.changeFeature += OnChangeFeature;

                AAState.createFeature += OnCreateFeature;
                AAState.manualFeature += OnManualFeature;
                localEventAdded = true;

            }

        }

        void ArcMap_NewOpenDocument()
        {

            try
            {
                AAState.WriteLine("New Doc");
                AAState.unInitEditing();
            }
            catch { }

            try
            {
                if (AAState._sw != null)
                {
                    AAState._sw.Flush();
                    AAState._sw.Close();
                    AAState._sw = null;
                }

            }
            catch { }
            bool curVal = AAState._clearLastValue;
            AAState._clearLastValue = true;
            init();
            AAState._clearLastValue = curVal;

        }
        void ArcMap_CloseDocument()
        {

            AAState.PerformUpdates = false;
            AAState.WriteLine("Attribute Assistant is off - closing doc");
            AAState.unInitEditing();
        }
        private void reloadOccured(object sender, EventArgs e)
        {
            GetConfigSettings();
            if (!_enabledOnStart | AAState._dt == null)
            {
                AAState.PerformUpdates = false;
                AAState.WriteLine("Attribute Assistant is off - reload");

            }
        }
        protected override void OnStartup()
        {
            ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_Event appStatusEvents = ArcMap.Application as ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_Event;
            appStatusEvents.Initialized += new ESRI.ArcGIS.ArcMap.IApplicationStatusEvents_InitializedEventHandler(appStatusEvents_Initialized);

            ArcMap.Events.NewDocument += ArcMap_NewOpenDocument;
            ArcMap.Events.OpenDocument += ArcMap_NewOpenDocument;
            ArcMap.Events.CloseDocument += ArcMap_CloseDocument;
            ReloadMonitor.reloadConfig += new ReloadEventHandler(reloadOccured);


            GetConfigSettings();
            if (!_enabledOnStart | AAState._dt == null)
            {
                AAState.PerformUpdates = false;
                AAState.WriteLine("Attribute Assistant is off - startup");

            }
            IEditor _editor = Globals.getEditor(ArcMap.Application);

            //Wire editor events.
            AAState._editEvents = (IEditEvents_Event)_editor;
            AAState._editEvents.OnStartEditing += OnStartEditing;
            AAState._editEvents.OnStopEditing += OnStopEditing;



            script = new MSScriptControl.ScriptControlClass();
            script.AllowUI = false;
            script.Language = "VBScript";
            script.UseSafeSubset = true;

            string strScript = "function iif(psdStr, trueStr, falseStr)" + System.Environment.NewLine +
                "On Error Resume Next" + System.Environment.NewLine +
                "if psdStr then" + System.Environment.NewLine +
                        "iif = trueStr" + System.Environment.NewLine +
                      "else " + System.Environment.NewLine +
                        "iif = falseStr" + System.Environment.NewLine +
                      "end if" + System.Environment.NewLine +
                    "end function" + System.Environment.NewLine;
            script.AddCode(strScript);


            AAState.WriteLine("                  script to process: " + newValue);


        }

        protected override void OnShutdown()
        {
            try
            {
                script = null;
                lastValue = null;




                intersectLayer = null;

                intersectTable = null;
                intersectLayerSelection = null;
                intersectTableSelection = null;
                qFilter = null;
                row = null;
                testField = null;

                curve = null;
                sourceLayer = null;
                pFS = null;

                _copyPoint = null;
                _copyPolyline = null;
                _copyPolygon = null;
                sFilter = null;
                try
                {
                    if (fCursor != null)
                        Marshal.ReleaseComObject(fCursor);
                    if (cCurs != null)
                        Marshal.ReleaseComObject(cCurs);
                    if (sourceFeature != null)
                        Marshal.ReleaseComObject(sourceFeature);

                }
                catch
                { }
                fCursor = null;
                cCurs = null;


                sourceFeature = null;
                nearestFeature = null;
                fieldObj = null;
                _currentDataset = null;
                proxOp = null;

            }
            catch { }
            try
            {
                if (AAState._sw != null)
                {
                    AAState._sw.Flush();
                    AAState._sw.Close();
                    AAState._sw = null;
                }

            }
            catch { }
            try
            {
                if (ArcMap.Events != null)
                {
                    ArcMap.Events.NewDocument -= ArcMap_NewOpenDocument;
                    ArcMap.Events.OpenDocument -= ArcMap_NewOpenDocument;
                }
                if (AAState._editor != null)
                {

                    if (AAState._editor != null)
                    {

                        //Wire editor events.
                        AAState._editEvents = (IEditEvents_Event)AAState._editor;
                        AAState._editEvents.OnStartEditing -= OnStartEditing;
                        AAState._editEvents.OnStopEditing -= OnStopEditing;

                        AAState._editor = null;
                        AAState._editEvents = null;

                    }
                }



                try
                {
                    AAState.bmpOff.Dispose();
                    AAState.bmpOn.Dispose();
                    AAState.commandItem = null;
                }
                catch { }
            }

            catch (Exception ex)
            {
                MessageBox.Show("OnShutdown: " + ex.Message);

            }
        }
        protected override bool OnSetState(ESRI.ArcGIS.Desktop.AddIns.ExtensionState state)
        {
            //Users can optionally check license here.
            this.State = state;

            AAState.setIcon();

            return true;
        }

        protected override ESRI.ArcGIS.Desktop.AddIns.ExtensionState OnGetState()
        {
            return this.State;
        }

        internal bool IsExtensionEnabled
        {
            get
            {
                return this.State == ESRI.ArcGIS.Desktop.AddIns.ExtensionState.Enabled;
            }
        }

        #region Editor Events

        #region Shortcut properties to the various editor event interfaces
        private IEditEvents_Event Events
        {
            get
            {

                return Globals.getEditor(ArcMap.Application) as IEditEvents_Event;

            }
        }
        private IEditEvents2_Event Events2
        {
            get { return Globals.getEditor(ArcMap.Application) as IEditEvents2_Event; }
        }
        private IEditEvents3_Event Events3
        {
            get { return Globals.getEditor(ArcMap.Application) as IEditEvents3_Event; }
        }
        private IEditEvents4_Event Events4
        {
            get { return Globals.getEditor(ArcMap.Application) as IEditEvents4_Event; }
        }
        #endregion

        void WireEditorEvents()
        {
            try
            {

                Events.OnCurrentTaskChanged += delegate
                {
                    if (Globals.getEditor(ArcMap.Application).CurrentTask != null)
                        System.Diagnostics.Debug.WriteLine(Globals.getEditor(ArcMap.Application).CurrentTask.Name);
                };
                Events2.BeforeStopEditing += delegate(bool save) { OnBeforeStopEditing(save); };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in the Wire Editor Events: " + ex.Message);

            }
        }

        void OnBeforeStopEditing(bool save)
        {
        }

        private void OnStartEditing()
        {

            try
            {
                AAState.initDynTable();



                AAState.WriteLine("Attribute Assistant start editing");
                if (AAState.PerformUpdates && AAState.Debug().ToUpper() == "TRUE")
                {
                    if (AAState._sw == null && AAState._filePath != "")
                    {

                        AAState._sw = Globals.createTextFile(AAState._filePath, FileMode.Append);

                        AAState.WriteLine("#######################################################");
                        Globals.LogLocations = AAState._filePath;

                    }

                }

                if (AAState._editor != null)
                {
                    if (AAState._editor.EditWorkspace != null)
                    {
                        IWorkspaceEditControl pWorkspaceEditControl = AAState._editor.EditWorkspace as IWorkspaceEditControl;
                        pWorkspaceEditControl.SetStoreEventsRequired();
                        pWorkspaceEditControl = null;
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error On Start Editing Event: " + ex.Message);

            }


        }
        private void OnStopEditing(Boolean save)
        {

        }



        private void OnChangeFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            IFeatureChanges pFeatChange = null;


            try
            {
                inFeature = obj as IFeature;


                if (inFeature != null)
                {

                    pFeatChange = (IFeatureChanges)inFeature;
                    if (pFeatChange.ShapeChanged)
                    {
                        sendEvent(obj, "ON_CHANGEGEO");
                    }
                    else
                    {
                        sendEvent(obj, "ON_CHANGE");
                    }
                }
                else
                {
                    sendEvent(obj, "ON_CHANGE");
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("OnChangeFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);

            }
            finally
            {
                inFeature = null;

            }
        }

        private void OnCreateFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {
                inFeature = obj as IFeature;
                sendEvent(obj, "ON_CREATE");
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnCreateFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);
            }
        }
        private void OnManualFeature(ESRI.ArcGIS.Geodatabase.IObject obj)
        {
            try
            {
                inFeature = obj as IFeature;


                sendEvent(obj, "ON_MANUAL");
            }
            catch (Exception ex)
            {
                MessageBox.Show("OnManualFeature:" + ex.Message + " \n" + obj.Class.AliasName + ": " + obj.OID);
            }
        }
        private void sendEvent(IObject inObject, string mode)
        {
            if (AAState._Suspend == true)
            {
                AAState.WriteLine("Attribute Assistant is suspended");

                return;
            }
            try
            {
                if (AAState.PerformUpdates == false)
                {
                    AAState.WriteLine("Attribute Assistant Send Even Perform Updates is off");

                    return;
                }
                AAState.WriteLine("Process Count: " + AAState._processCount);
                if (_LastOID == inObject.OID && _LastMode == mode && _LastFC == inObject.Class.AliasName && AAState._processCount > 0)
                {
                    AAState.WriteLine("Last Feature matches and the there is a process count");
                    _LastOID = -1;
                    _LastMode = "";
                    _LastFC = "";
                    return;
                }
                _LastOID = inObject.OID;
                _LastMode = mode;
                _LastFC = inObject.Class.AliasName;

                try
                {
                    AAState.WriteLine("Unwiring the events");

                    AAState.changeFeature -= OnChangeFeature;
                    AAState.createFeature -= OnCreateFeature;


                    if (AAState.Debug().ToUpper() == "TRUE")
                    {
                        if (AAState._sw == null && AAState._filePath != "")
                        {

                            AAState._sw = Globals.createTextFile(AAState._filePath, FileMode.Append);
                            Globals.LogLocations = AAState._filePath;

                        }
                        AAState.WriteLine("#######################################################");

                        AAState.WriteLine(inObject.Class.AliasName + " - " + mode.ToString());
                    }
                    List<IObject> newFeatureList = null;
                    List<IObject> changedFeatureList = null;
                    List<IObject> changedFeatureGeoList = null;
                    try
                    {
                        AAState._processCount++;
                        //Set attributes based on the dynamic defaults configuration table
                        bool success = SetDynamicValues(inObject, mode, out changedFeatureList, out newFeatureList, out changedFeatureGeoList);
                        if (!success)
                        {

                            // MessageBox.Show("Attribute Assistant has failed or was cancelled");
                            return;
                        }
                        else
                        {
                            if (changedFeatureList != null)
                            {
                                processFeatures(changedFeatureList, "ON_CHANGE");
                            }
                            if (changedFeatureGeoList != null)
                            {
                                processFeatures(changedFeatureGeoList, "ON_CHANGEGEO");
                            }
                            if (newFeatureList != null)
                            {
                                processFeatures(newFeatureList, "ON_CREATE");
                            }

                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        AAState._processCount--;
                        try
                        {
                            if (changedFeatureList != null)
                            {
                                changedFeatureList.Clear();
                            }
                        }
                        catch
                        { }
                        changedFeatureList = null;
                        try
                        {
                            if (newFeatureList != null)
                            {
                                newFeatureList.Clear();
                            }
                        }
                        catch
                        { }
                        newFeatureList = null;

                        try
                        {
                            if (changedFeatureGeoList != null)
                            {
                                changedFeatureGeoList.Clear();
                            }
                        }
                        catch
                        { }
                        changedFeatureGeoList = null;
                        if (AAState._sw != null)
                        {
                            if (AAState._processCount == 0)
                            {
                                AAState.WriteLine("#######################################################");
                                AAState._sw.Flush();

                            }
                        }

                    }

                }
                finally
                {
                    AAState.WriteLine("Wiring the events");
                    AAState.changeFeature += OnChangeFeature;
                    AAState.createFeature += OnCreateFeature;
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on the Send Event Object: " + ex.Message);

            }
        }

        private void processFeatures(List<IObject> ObjectList, string eventType)
        {
            if (ObjectList != null)
            {
                List<IObject> newFeatureList = null;
                List<IObject> changedFeatureList = null;
                List<IObject> changedFeatureGeoList = null;

                try
                {
                    foreach (IObject inObject in ObjectList)
                    {
                        AAState.WriteLine("#######################################################");
                        AAState.WriteLine(inObject.Class.AliasName + " - Created through AA rules - Running " + eventType);

                        try
                        {
                            AAState._processCount++;
                            //Set attributes based on the dynamic defaults configuration table
                            bool success = SetDynamicValues(inObject, eventType, out changedFeatureList, out newFeatureList, out changedFeatureGeoList);
                            try
                            {
                                inObject.Store();
                            }
                            catch
                            { 
                            
                            }
                            if (!success)
                            {

                                // MessageBox.Show("Attribute Assistant has failed or was cancelled");
                                return;
                            }
                            else
                            {
                                if (changedFeatureList != null)
                                {
                                    processFeatures(changedFeatureList, "ON_CHANGE");
                                }
                                if (changedFeatureGeoList != null)
                                {
                                    processFeatures(changedFeatureGeoList, "ON_CHANGEGEO");
                                }
                                if (newFeatureList != null)
                                {
                                    processFeatures(newFeatureList, "ON_CREATE");
                                }

                            }
                        }
                        catch
                        {

                        }
                        finally
                        {
                            AAState._processCount--;
                            try
                            {
                                if (changedFeatureList != null)
                                {
                                    changedFeatureList.Clear();
                                }
                            }
                            catch
                            { }
                            changedFeatureList = null;
                            try
                            {
                                if (newFeatureList != null)
                                {
                                    newFeatureList.Clear();
                                }
                            }
                            catch
                            { }
                            newFeatureList = null;

                            try
                            {
                                if (changedFeatureGeoList != null)
                                {
                                    changedFeatureGeoList.Clear();
                                }
                            }
                            catch
                            { }
                            changedFeatureGeoList = null;

                            if (AAState._sw != null)
                            {
                                if (AAState._processCount == 0)
                                {
                                    AAState.WriteLine("#######################################################");
                                    AAState._sw.Flush();

                                }
                            }

                        }



                    }

                }
                catch
                {
                }
                finally
                {
                    try
                    {
                        if (changedFeatureList != null)
                        {
                            changedFeatureList.Clear();
                        }
                    }
                    catch
                    { }
                    changedFeatureList = null;
                    try
                    {
                        if (newFeatureList != null)
                        {
                            newFeatureList.Clear();
                        }
                    }
                    catch
                    { }
                    newFeatureList = null;

                    try
                    {
                        if (changedFeatureGeoList != null)
                        {
                            changedFeatureGeoList.Clear();
                        }
                    }
                    catch
                    { }
                    changedFeatureGeoList = null;
                }
            }
        }

        #endregion

        #region Helper Methods

        public bool SetDynamicValues(IObject inObject, string mode, out List<IObject> ChangeFeatureList, out List<IObject> NewFeatureList, out List<IObject> ChangeFeatureGeoList)
        {
            ChangeFeatureList = null;
            NewFeatureList = null;
            ChangeFeatureGeoList = null;

            IMSegmentation mseg = null;
            INetworkFeature netFeat = null;

            IJunctionFeature iJuncFeat = null;
            IEdgeFeature iEdgeFeat = null;
            //ProgressBar
            ESRI.ArcGIS.Framework.IProgressDialogFactory progressDialogFactory = null;
            ESRI.ArcGIS.esriSystem.IStepProgressor stepProgressor = null;
            ESRI.ArcGIS.Framework.IProgressDialog2 progressDialog = null;
            // Create a CancelTracker
            ESRI.ArcGIS.esriSystem.ITrackCancel trackCancel = new ESRI.ArcGIS.Display.CancelTrackerClass();
            trackCancel.CancelOnKeyPress = true;
            // trackCancel.CancelOnClick = true;

            try
            {
                AAState.WriteLine("Starting AA");
                if (AAState.PerformUpdates == false)
                {
                    AAState.WriteLine("Perform Updates is false");

                }
                if (AAState.PerformUpdates && AAState._dv == null)
                {

                    AAState.WriteLine("Dynamic Value Table is missing - Name in Config:" + AAState._defaultsTableName);


                    AAState.PerformUpdates = false;
                    return false;
                }

                if (AAState.PerformUpdates && AAState._dv != null)
                {
                    if (AAState._dv.Table == null)
                    {

                        AAState.WriteLine("the dv.table is null");

                        return false;
                    }

                    if (inObject == null)
                    {
                        AAState.WriteLine("Input object is null");

                        return false;
                    }
                    if (inObject.Table == AAState._tab)
                    {

                        if (inObject.get_Value(inObject.Fields.FindField("VALUEMETHOD")).ToString().ToUpper() == "LAST_VALUE")
                        {
                            if (inObject.get_Value(inObject.Fields.FindField("FIELDNAME")) != null)
                            {
                                if (inObject.get_Value(inObject.Fields.FindField("FIELDNAME")) != DBNull.Value)
                                {
                                    try
                                    {
                                        LastValueEntry lstVal = AAState.lastValueProperties.GetProperty(inObject.get_Value(inObject.Fields.FindField("FIELDNAME")).ToString()) as LastValueEntry;
                                        if (lstVal != null)
                                        {
                                            lstVal.On_Manual = Globals.toBoolean(inObject.get_Value(inObject.Fields.FindField("ON_MANUAL")).ToString());
                                            lstVal.On_Create = Globals.toBoolean(inObject.get_Value(inObject.Fields.FindField("ON_CREATE")).ToString());
                                            lstVal.On_ChangeAtt = Globals.toBoolean(inObject.get_Value(inObject.Fields.FindField("ON_CHANGE")).ToString());

                                            if (inObject.Fields.FindField("ON_CHANGEGEO") >= 0)
                                            {
                                                lstVal.On_ChangeGeo = Globals.toBoolean(inObject.get_Value(inObject.Fields.FindField("ON_CHANGEGEO")).ToString());
                                            }
                                        }
                                        AAState.WriteLine("Last Value rule was modified, updating Last Value Array");
                                    }
                                    catch
                                    {
                                        //property does not exist

                                    }
                                }
                            }



                        }
                        else
                        {
                            AAState.WriteLine("Dynamic Value Table is the table that is edited, skipping");
                        }
                        return false;
                    }

                    string modeVal;

                    if (AAState._dv.Table.Columns[mode] == null)
                    {
                        AAState.WriteLine(mode + " column is missing");
                        mode = mode.Replace("GEO", "");

                    }
                    if (AAState._dv.Table.Columns[mode].DataType == System.Type.GetType("System.String"))
                    {
                        modeVal = "(" + mode + " = '1' or " + mode + " = 'Yes' or " + mode + " = 'YES' or " + mode + " = 'True' or " + mode + " = 'TRUE')";

                    }
                    else
                    {
                        modeVal = mode + " = 1";
                    }
                    System.Int32 int32_hWnd = ArcMap.Application.hWnd;

                    progressDialogFactory = new ESRI.ArcGIS.Framework.ProgressDialogFactoryClass();
                    stepProgressor = progressDialogFactory.Create(trackCancel, int32_hWnd);

                    stepProgressor.MinRange = 0;
                    stepProgressor.MaxRange = inObject.Fields.FieldCount;
                    stepProgressor.StepValue = 1;
                    stepProgressor.Message = "Attribute Assistant Progress";
                    // Create the ProgressDialog. This automatically displays the dialog
                    progressDialog = (ESRI.ArcGIS.Framework.IProgressDialog2)stepProgressor; // Explict Cast

                    // Set the properties of the ProgressDialog
                    progressDialog.CancelEnabled = true;
                    progressDialog.Description = "Checking rules for " + inObject.Class.AliasName;
                    progressDialog.Title = "Attribute Assistant Progress";
                    progressDialog.Animation = ESRI.ArcGIS.Framework.esriProgressAnimationTypes.esriProgressGlobe;
                    progressDialog.ShowDialog();

                    //Optional skip junctions feature class
                    //if (Globals.isOrpanJunction(inFeature))
                    //    return false;

                    //Get table name for this feature
                    _currentDataset = inObject.Class as IDataset;
                    _currentDatasetNameItems = _currentDataset.Name.Split('.');
                    tableName = _currentDatasetNameItems[_currentDatasetNameItems.GetLength(0) - 1];

                    stepProgressor.Message = "Checking rules for edited feature: " + inObject.Class.AliasName;
                    progressDialog.Description = "Checking rules for edited feature: " + inObject.Class.AliasName;
                    AAState.WriteLine("***********************************************************");
                    AAState.WriteLine("############ " + DateTime.Now + " ################");

                    AAState.WriteLine("");
                    AAState.WriteLine("  Setting sort order: Field - RUNORDER");

                    if (AAState._dv.Table.Columns.Contains("RUN_WEIGHT"))
                        AAState._dv.Sort = "RUN_WEIGHT DESC";




                    AAState.WriteLine("  Querying table for Last Value for layer: " + inObject.Class.AliasName);
                    AAState.WriteLine("  Query Used: (TABLENAME = '*' OR TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') AND VALUEMETHOD = 'Last_Value'");
                    AAState._dv.RowFilter = "(TABLENAME = '*' OR TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') AND VALUEMETHOD = 'Last_Value'";
                    AAState.WriteLine("  Number of results: " + AAState._dv.Count.ToString());

                    if (AAState._dv.Count > 0)
                    {
                        IRowChanges pRowChLast = inObject as IRowChanges;
                        for (int retRows = 0; retRows < AAState._dv.Count; retRows++)
                        {
                            DataRowView drv = AAState._dv[retRows];
                            AAState.WriteLine("       Looking for " + drv["FIELDNAME"].ToString());

                            int fldLoc = inObject.Fields.FindField(drv["FIELDNAME"].ToString());

                            if (fldLoc > 0)
                            {
                                AAState.WriteLine("       " + drv["FIELDNAME"].ToString() + " field found at position: " + fldLoc);

                                if (pRowChLast.get_ValueChanged(fldLoc))
                                {
                                    AAState.WriteLine("       " + drv["FIELDNAME"].ToString() + " Has Changed");

                                    try
                                    {
                                        LastValueEntry lstVal = (AAState.lastValueProperties.GetProperty(drv["FIELDNAME"].ToString()) as LastValueEntry);

                                        if (lstVal != null)
                                        {


                                            if (inObject.get_Value(fldLoc) != null)
                                            {
                                                if (inObject.get_Value(fldLoc) != DBNull.Value)
                                                {


                                                    if (lstVal.Value != null)
                                                    {
                                                        AAState.WriteLine("                      Setting Last Value");
                                                        AAState.WriteLine("                           " + drv["FIELDNAME"].ToString() + ": " + inObject.get_Value(fldLoc).ToString());
                                                        lstVal.Value = inObject.get_Value(fldLoc);

                                                        AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstVal);
                                                    }

                                                    else
                                                    {
                                                        AAState.WriteLine("                      Setting Last Value");
                                                        AAState.WriteLine("                           " + drv["FIELDNAME"].ToString() + ": " + inObject.get_Value(fldLoc));
                                                        lstVal.Value = inObject.get_Value(fldLoc);

                                                        AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstVal);
                                                    }

                                                }
                                                else
                                                {
                                                    if (mode == "ON_CREATE")
                                                    {
                                                        AAState.WriteLine("                      Skipping null on create");

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                      Setting Last Value");
                                                        AAState.WriteLine("                           " + drv["FIELDNAME"].ToString() + ": NULL");
                                                        lstVal.Value = null;
                                                        AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstVal);
                                                    }

                                                }
                                            }
                                            else
                                            {
                                                if (mode == "ON_CREATE")
                                                {
                                                    AAState.WriteLine("                      Skipping null on create");

                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                      Setting Last Value");
                                                    AAState.WriteLine("                           " + drv["FIELDNAME"].ToString() + ": NULL");
                                                    lstVal.Value = null;
                                                    AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstVal);

                                                }
                                            }

                                        }
                                        else
                                        {
                                            lstVal = new LastValueEntry();
                                            lstVal.Value = inObject.get_Value(fldLoc);

                                            lstVal.On_Manual = Globals.toBoolean(drv["ON_MANUAL"].ToString());
                                            lstVal.On_Create = Globals.toBoolean(drv["ON_CREATE"].ToString());
                                            lstVal.On_ChangeAtt = Globals.toBoolean(drv["ON_CHANGE"].ToString());
                                            if (drv["ON_CHANGEGEO"] != null)
                                            {
                                                lstVal.On_ChangeGeo = Globals.toBoolean(drv["ON_CHANGEGEO"].ToString());
                                            }
                                            AAState.lastValueProperties.SetProperty(drv["FIELDNAME"].ToString(), lstVal);

                                        }

                                    }
                                    catch
                                    {


                                    }


                                }
                                else
                                {
                                    AAState.WriteLine("       " + drv["FIELDNAME"].ToString() + " Has not changed");

                                }
                            }
                        }

                        pRowChLast = null;

                    }







                    AAState.WriteLine("  Querying table for rules for layer: " + inObject.Class.AliasName);
                    AAState.WriteLine("  Query Used: (TABLENAME = '*' OR TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') AND " + modeVal);
                    AAState._dv.RowFilter = "(TABLENAME = '*' OR TABLENAME = '" + tableName + "' OR TABLENAME like '" + tableName + "|*' OR TABLENAME like '" + tableName + "|%') AND " + modeVal;
                    AAState.WriteLine("  Number of results: " + AAState._dv.Count.ToString());
                    if (AAState._processCount > 2)
                    {
                        System.Threading.Thread.Sleep(400);
                    }
                    if (AAState._processCount > 15)
                    {
                        MessageBox.Show("You have more than 15 processes running, more than likely your rules are causing an infinite loop.  Any rule that creates a new feature with * as the table name can cause this issue.");

                        return false;

                    }
                    if (AAState._dv.Count > 0)
                    {
                        bool proc = false;
                        AAState.WriteLine("  Looping through the rows");

                        for (int retRows = 0; retRows < AAState._dv.Count; retRows++)
                        {
                            DataRowView drv = AAState._dv[retRows];

                            AAState.WriteLine("    ------------------------------------------------");
                            AAState.WriteLine("      Row Info");
                            AAState.WriteLine("        Row Number " + (retRows + 1).ToString());
                            AAState.WriteLine("        TableName: " + drv["TABLENAME"].ToString());
                            AAState.WriteLine("        FieldName: " + drv["FIELDNAME"].ToString());
                            AAState.WriteLine("        ValueInfo: " + drv["VALUEINFO"].ToString());
                            AAState.WriteLine("        ValueMethod: " + drv["VALUEMETHOD"].ToString());
                            AAState.WriteLine("        On Create: " + drv["ON_CREATE"].ToString());
                            AAState.WriteLine("        On Change: " + drv["ON_CHANGE"].ToString());
                            if (AAState._dv.Table.Columns.Contains("RUNORDER"))
                                AAState.WriteLine("        Order: " + drv["RUNORDER"].ToString());

                            AAState.WriteLine("");

                            AAState.WriteLine("      Checking for Subtype Restriction");
                            valFC = drv["TABLENAME"].ToString().Trim();
                            if (valFC.Contains("|"))
                            {
                                AAState.WriteLine("        Subtype restriction Found");
                                string[] spliVal = valFC.Split('|');
                                List<string> validSubtypes = new List<string>(spliVal[1].Split(','));
                                List<string> inValidSubtypes;
                                if (spliVal.GetLength(0) == 3)
                                {
                                    inValidSubtypes = new List<string>(spliVal[2].Split(','));
                                }
                                else
                                {
                                    inValidSubtypes = new List<string>();
                                }

                                int obSubVal;
                                ISubtypes pSub = inObject.Class as ISubtypes;
                                if (pSub != null)
                                {
                                    if (pSub.HasSubtype)
                                    {
                                        if (inObject.get_Value(pSub.SubtypeFieldIndex).ToString() != "")
                                        {
                                            obSubVal = Convert.ToInt32(inObject.get_Value(pSub.SubtypeFieldIndex).ToString());
                                            if (validSubtypes.Contains("*"))
                                            {
                                                if (inValidSubtypes.Contains(obSubVal.ToString()))
                                                {
                                                    AAState.WriteLine("        Skipping, not the subtype defined");
                                                    proc = false;
                                                    continue;
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("        Subtype is valid");
                                                }
                                            }

                                            else if (validSubtypes.Contains(obSubVal.ToString()))
                                            {
                                                AAState.WriteLine("        Subtype is valid");
                                            }
                                            else
                                            {
                                                AAState.WriteLine("        Skipping, not the subtype defined");
                                                proc = false;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            AAState.WriteLine("        Skipping, subtype is not set");
                                            proc = false;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        AAState.WriteLine("        ERROR: Layer does not have subtypes");
                                    }
                                }
                                else
                                {
                                    AAState.WriteLine("        ERROR: Layer does not have subtypes");
                                }

                            }
                            valMethod = drv["VALUEMETHOD"].ToString().ToUpper().Trim();
                            valData = drv["VALUEINFO"].ToString().Trim();
                            if (valData.Contains(Environment.NewLine))
                            {
                                valData = valData.Substring(0, valData.IndexOf(Environment.NewLine));


                            }
                            List<string> strFldNames = new List<string>();
                            List<string> strFldAlias = new List<string>();
                            List<int> intFldIdxs = new List<int>();
                            fieldObj = null;
                            if (drv["FIELDNAME"] != null && drv["FIELDNAME"].ToString().Trim() != "" && drv["FIELDNAME"].ToString().Trim() != "*" && drv["FIELDNAME"].ToString().Trim() != "#")
                            {
                                strFldNames = new List<string>(drv["FIELDNAME"].ToString().Trim().Split(','));
                                foreach (string strFldName in strFldNames)
                                {
                                    if (inObject.Fields.FindField(strFldName) >= 0)
                                    {
                                        strFldAlias.Add(inObject.Fields.get_Field(inObject.Fields.FindField(strFldName)).AliasName);

                                        int tem = inObject.Fields.FindField(strFldName);
                                        intFldIdxs.Add(tem);
                                        fieldObj = inObject.Fields.get_Field(tem);
                                        AAState.WriteLine("      Field Name: " + inObject.Fields.get_Field(inObject.Fields.FindField(strFldName)).AliasName + " was found at index: " + tem);

                                        proc = true;
                                    }
                                    else if (inObject.Fields.FindFieldByAliasName(strFldName) >= 0)
                                    {
                                        int tem = inObject.Fields.FindFieldByAliasName(strFldName);
                                        intFldIdxs.Add(tem);
                                        strFldAlias.Add(inObject.Fields.get_Field(inObject.Fields.FindField(strFldName)).AliasName);

                                        fieldObj = inObject.Fields.get_Field(tem);

                                        AAState.WriteLine("      Field Name: " + strFldName + " was found at index: " + tem);

                                        proc = true;
                                    }
                                    else
                                    {
                                        intFldIdxs.Add(-1);
                                        strFldAlias.Add("{Not Found}");

                                        AAState.WriteLine("      " + strFldName + " Field not found");

                                        fieldObj = null;
                                        proc = false;
                                    }
                                }
                            }
                            else if (drv["FIELDNAME"].ToString() == "#")
                            {
                                AAState.WriteLine("      Field is set to edited field");
                                IRowChanges pRowCh = null;
                                IField pTmpFld = null;
                                pRowCh = inObject as IRowChanges;


                                for (int i = 0; i < inObject.Fields.FieldCount; i++)
                                {
                                    pTmpFld = inObject.Fields.get_Field(i);

                                    if (pTmpFld.Type != esriFieldType.esriFieldTypeGlobalID &&
                                                               pTmpFld.Type != esriFieldType.esriFieldTypeOID &&
                                                               pTmpFld.Type != esriFieldType.esriFieldTypeGeometry &&
                                                                pTmpFld.Name.ToUpper() != "SHAPE_LENGTH" &&
                                                               pTmpFld.Name.ToUpper() != "SHAPE.LEN" &&
                                                               pTmpFld.Name.ToUpper() != "SHAPE_AREA" &&
                                                               pTmpFld.Name.ToUpper() != "SHAPE.AREA")
                                    {
                                        if (pRowCh.get_ValueChanged(i) == true)
                                        {
                                            AAState.WriteLine("      Adding " + pTmpFld.Name + " to field array");
                                            strFldNames.Add(pTmpFld.Name);
                                            intFldIdxs.Add(i);
                                            proc = true;


                                        }
                                    }

                                }
                                pRowCh = null;
                                pTmpFld = null;
                            }
                            else
                            {
                                AAState.WriteLine("      Field is not specified, empty, or set for all.");
                                fieldObj = null;
                                proc = true;
                            }

                            if (proc)
                            {

                                try
                                {
                                    switch (valMethod)
                                    {



                                        case "FIELD_TRIGGER"://Value|FieldToChange|Value

                                            try
                                            {
                                                AAState.WriteLine("                  Trying: FIELD_TRIGGER");
                                                if (inFeature != null & valData != null)
                                                {

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    string valToCheck = "";
                                                    string fldToChange = "";
                                                    string valToSet = "";
                                                    if (args.GetLength(0) == 3)
                                                    {
                                                        valToCheck = args[0];
                                                        fldToChange = args[1];
                                                        valToSet = args[2];

                                                    }

                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Incorrect Value info was not found");
                                                        continue;
                                                    }

                                                    IRowChanges pRowCh = null;

                                                    pRowCh = inObject as IRowChanges;

                                                    if (intFldIdxs.Count == 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Field Not Set");
                                                        continue;
                                                    }
                                                    if (intFldIdxs[0] == -1)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Field Not Found");
                                                        continue;
                                                    }
                                                    if (pRowCh.get_ValueChanged(intFldIdxs[0]))
                                                    {
                                                        AAState.WriteLine("                  Listed field changed");
                                                        if (inObject.get_Value(intFldIdxs[0]).ToString() == valToCheck)
                                                        {
                                                            AAState.WriteLine("                  Values Match");
                                                            int chngFldIdx = Globals.GetFieldIndex(inObject.Fields, fldToChange);
                                                            if (chngFldIdx == -1)
                                                            {
                                                                AAState.WriteLine("                  ERROR: Field Not Found");
                                                                continue;
                                                            }
                                                            try
                                                            {
                                                                inObject.set_Value(chngFldIdx, valToSet);
                                                                AAState.WriteLine("                  Value Set");
                                                            }
                                                            catch
                                                            {
                                                                AAState.WriteLine("                  ERROR: Could not store Value: " + valToSet);
                                                            }

                                                        }
                                                    }
                                                    pRowCh = null;

                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: FIELD_TRIGGER: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: FIELD_TRIGGER");
                                            }
                                            break;
                                        case "VALIDATE_ATTRIBUTE_LOOKUP":
                                            {
                                                AAState.WriteLine("                  Trying VALIDATE_ATTRIBUTE_LOOKUP");
                                                IRowChanges pRowCh = null;
                                                ISQLSyntax sqlSyntax = null;
                                                IQueryFilter pQFilt = null;
                                                try
                                                {
                                                    if ((valData != null) && (inObject != null))
                                                    {

                                                        pRowCh = inObject as IRowChanges;
                                                        bool valueChanged = false;
                                                        for (int i = 0; i < intFldIdxs.Count; i++)
                                                        {
                                                            if (pRowCh.get_ValueChanged(intFldIdxs[i]) == true)
                                                            {
                                                                valueChanged = true;
                                                                break;

                                                            }
                                                        }
                                                        if (valueChanged == false)
                                                        {
                                                            AAState.WriteLine("                  VALIDATE_ATTRIBUTE_LOOKUP: Target value(s) did not change, skipping");
                                                            continue;
                                                        }
                                                        sourceLayerName = "";
                                                        string[] sourceFieldNames = null;


                                                        // Parse arguments
                                                        args = valData.Split('|');
                                                        if (args.Length == 2)
                                                        {
                                                            sourceLayerName = args[0].ToString().Trim();
                                                            sourceFieldNames = args[1].ToString().Split(',');

                                                        }

                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR:  the valuemethod is not defined properly for this rule");
                                                            continue;

                                                        }

                                                        if ((sourceFieldNames != null) &&
                                                            (sourceFieldNames.Length > 0))
                                                        {
                                                            AAState.WriteLine("                  Looking for layer: " + sourceLayerName);

                                                            boolLayerOrFC = true;
                                                            if (sourceLayerName.Contains("("))
                                                            {
                                                                string[] tempSplt = sourceLayerName.Split('(');
                                                                sourceLayerName = tempSplt[0];
                                                                sourceLayer = Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC) as IFeatureLayer;
                                                                if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                            }
                                                            else
                                                            {


                                                                sourceLayer = Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC) as IFeatureLayer;
                                                            }

                                                            IFields pFlds = null;

                                                            IDataset pDs = null;
                                                            string layNameFnd = "";
                                                            bool matchingLayFnd = false;
                                                            IStandaloneTable pTbl = null;
                                                            if (sourceLayer != null)
                                                            {
                                                                if (sourceLayer.FeatureClass != null)
                                                                {
                                                                    pFlds = sourceLayer.FeatureClass.Fields;
                                                                    pDs = sourceLayer.FeatureClass as IDataset;
                                                                    layNameFnd = sourceLayer.Name;
                                                                    matchingLayFnd = true;
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayerName + " data source is not set");
                                                                    continue;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                pTbl = Globals.FindStandAloneTable(AAState._editor.Map, sourceLayerName) as IStandaloneTable;

                                                                if (pTbl != null)
                                                                {
                                                                    if (pTbl.Table != null)
                                                                    {
                                                                        pFlds = pTbl.Table.Fields;
                                                                        pDs = pTbl.Table as IDataset;
                                                                        layNameFnd = pTbl.Name;

                                                                        matchingLayFnd = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayerName + " data source is not set");
                                                                    continue;
                                                                }

                                                            }
                                                            if (matchingLayFnd == false)
                                                            {
                                                                AAState.WriteLine("                  ERROR: " + sourceLayerName + " was not found");
                                                                continue;
                                                            }
                                                            sqlSyntax = (ISQLSyntax)(pDs.Workspace);
                                                            string specChar = sqlSyntax.GetSpecialCharacter(esriSQLSpecialCharacters.esriSQL_WildcardManyMatch);

                                                            AAState.WriteLine("                  Lookup layer " + layNameFnd + " was Found: " + sourceLayerName);

                                                            AAState.WriteLine("                  Checking for field in Lookup table");


                                                            if (sourceFieldNames.Length != intFldIdxs.Count)
                                                            {
                                                                AAState.WriteLine("                  Number of listed fields do not match");
                                                                continue;

                                                            }
                                                            int[] sourceFieldNums = new int[sourceFieldNames.Length];


                                                            pQFilt = new QueryFilterClass();
                                                            string sqlString = "";
                                                            string sqlStringUpper = "";
                                                            string sqlUpp = "";
                                                            if (sqlSyntax.GetStringComparisonCase())
                                                            {
                                                                sqlUpp = "UPPER";
                                                            }
                                                            for (int i = 0; i < sourceFieldNames.Length; i++)
                                                            {
                                                                sourceFieldNums[i] = Globals.GetFieldIndex(pFlds, sourceFieldNames[i].Trim());
                                                                if (sourceFieldNums[i] < 0)
                                                                {
                                                                    AAState.WriteLine("                  Fields Missing: " + sourceFieldName[i]);
                                                                    break;


                                                                }
                                                                if (pFlds.get_Field(sourceFieldNums[i]).Type == esriFieldType.esriFieldTypeString)
                                                                {
                                                                    if (sqlString == "")
                                                                    {
                                                                        sqlString = pFlds.get_Field(sourceFieldNums[i]).Name + "" + " = '" + inObject.get_Value(intFldIdxs[i]).ToString() + "'";
                                                                        sqlStringUpper = sqlUpp + "(" + pFlds.get_Field(sourceFieldNums[i]).Name + ")" + " LIKE '" + specChar + inObject.get_Value(intFldIdxs[i]).ToString().ToUpper().Replace(" ", specChar) + specChar + "'";
                                                                    }
                                                                    else
                                                                    {
                                                                        sqlString = sqlString + " AND " + pFlds.get_Field(sourceFieldNums[i]).Name + "" + " = '" + inObject.get_Value(intFldIdxs[i]).ToString() + "'";
                                                                        sqlStringUpper = sqlStringUpper + " AND " + sqlUpp + "(" + pFlds.get_Field(sourceFieldNums[i]).Name + ")" + " LIKE '" + specChar + inObject.get_Value(intFldIdxs[i]).ToString().ToUpper().Replace(" ", specChar) + specChar + "'";
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    if (sqlString == "")
                                                                    {
                                                                        sqlString = pFlds.get_Field(sourceFieldNums[i]).Name + " = " + inObject.get_Value(intFldIdxs[i]);
                                                                        sqlStringUpper = pFlds.get_Field(sourceFieldNums[i]).Name + " LIKE " + specChar + inObject.get_Value(intFldIdxs[i]) + specChar;
                                                                    }
                                                                    else
                                                                    {
                                                                        sqlString = sqlString + " AND " + pFlds.get_Field(sourceFieldNums[i]).Name + " = " + inObject.get_Value(intFldIdxs[i]);
                                                                        sqlStringUpper = sqlStringUpper + " AND " + pFlds.get_Field(sourceFieldNums[i]).Name + " LIKE " + specChar + inObject.get_Value(intFldIdxs[i]) + specChar;
                                                                    }
                                                                }

                                                            }
                                                            pQFilt.WhereClause = sqlString;



                                                            AAState.WriteLine("                  " + pQFilt.WhereClause + " used to search for matching record");
                                                            int intRecFound = 0;
                                                            if (sourceLayer == null)
                                                            {
                                                                intRecFound = pTbl.Table.RowCount(pQFilt);
                                                            }
                                                            else
                                                            {
                                                                intRecFound = sourceLayer.FeatureClass.FeatureCount(pQFilt);
                                                            }

                                                            AAState.WriteLine("                  " + intRecFound + " rows found using " + sqlString);

                                                            if (intRecFound != 1)
                                                            {

                                                                pQFilt.WhereClause = sqlStringUpper;



                                                                AAState.WriteLine("                  " + pQFilt.WhereClause + " used to search for matching record");
                                                                if (sourceLayer == null)
                                                                {
                                                                    intRecFound = pTbl.Table.RowCount(pQFilt);
                                                                }
                                                                else
                                                                {
                                                                    intRecFound = sourceLayer.FeatureClass.FeatureCount(pQFilt);
                                                                }
                                                                AAState.WriteLine("                  " + intRecFound + " rows found");


                                                                if (intRecFound == 0)
                                                                {
                                                                    AAState.WriteLine("                     Abort Edit");
                                                                    AAState._editor.AbortOperation();
                                                                    return false;


                                                                }

                                                                else
                                                                {
                                                                    AAState.WriteLine("                  Prompting for user input");
                                                                    ICursor pCurs = null;
                                                                    if (sourceLayer == null)
                                                                    {
                                                                        pCurs = pTbl.Table.Search(pQFilt, true);
                                                                    }
                                                                    else
                                                                    {
                                                                        pCurs = sourceLayer.FeatureClass.Search(pQFilt, true) as ICursor;
                                                                    }

                                                                    pQFilt = null;


                                                                    List<string> pLst = Globals.CursorToList(ref pCurs, sourceFieldNums);
                                                                    if (pCurs != null)
                                                                        Marshal.ReleaseComObject(pCurs);
                                                                    pCurs = null;

                                                                    string disFld = "";
                                                                    for (int j = 0; j < sourceFieldNames.Length - 1; j++)
                                                                    {
                                                                        disFld = disFld == "" ? sourceFieldNames[j] : disFld + "|" + sourceFieldNames[j];

                                                                    }
                                                                    string selectVal = Globals.showOptionsForm(pLst, "Select a valid value to store for " + disFld, ComboBoxStyle.DropDownList);
                                                                    if (selectVal == "||Cancelled||")
                                                                    {
                                                                        AAState.WriteLine("                     Abort Edit");
                                                                        AAState._editor.AbortOperation();
                                                                        return false;

                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  Value selected: " + selectVal);
                                                                        string[] strVals = selectVal.Split('|');

                                                                        for (int i = 0; i < sourceFieldNums.Length; i++)
                                                                        {
                                                                            inObject.set_Value(intFldIdxs[i], strVals[i]);
                                                                        }

                                                                    }


                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  One Exact match was found");
                                                            }
                                                            pQFilt = null;





                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Invalid Value Info: " + valData);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Not a feature or missing Value Info");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: VALIDATE_ATTRIBUTE_LOOKUP" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    pQFilt = null;
                                                    sqlSyntax = null;
                                                    pRowCh = null;
                                                    AAState.WriteLine("                  Finished: VALIDATE_ATTRIBUTE_LOOKUP");
                                                }
                                                break;
                                            }

                                        case "PREVIOUS_VALUE":
                                            {
                                                AAState.WriteLine("                  Trying PREVIOUS_VALUE");
                                                IRowChanges pRowCh = null;
                                                try
                                                {
                                                    if ((valData != null) && (inObject != null))
                                                    {

                                                        pRowCh = inObject as IRowChanges;
                                                        bool valueChanged = false;
                                                        for (int i = 0; i < intFldIdxs.Count; i++)
                                                        {
                                                            if (pRowCh.get_ValueChanged(intFldIdxs[i]) == true)
                                                            {
                                                                valueChanged = true;
                                                                break;

                                                            }
                                                        }
                                                        if (valueChanged == false)
                                                        {
                                                            AAState.WriteLine("                  PREVIOUS_VALUE: Target value(s) did not change, skipping");
                                                            continue;
                                                        }

                                                        string[] sourceFieldNames = null;


                                                        // Parse arguments
                                                        args = valData.Split('|');
                                                        if (args.Length == 1)
                                                        {

                                                            sourceFieldNames = args[0].ToString().Split(',');

                                                        }

                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR:  the valuemethod is not defined properly for this rule");
                                                            continue;

                                                        }

                                                        if ((sourceFieldNames != null) &&
                                                            (sourceFieldNames.Length > 0))
                                                        {
                                                            int flx = inObject.Fields.FindField(sourceFieldNames[0]);
                                                            if (flx > 0)
                                                            {
                                                                inObject.set_Value(flx, pRowCh.get_OriginalValue(intFldIdxs[0]));
                                                                inObject.Store();

                                                            }
                                                        }
                                                        else
                                                        {

                                                            AAState.WriteLine("                  ERROR: Invalid Value Info: " + valData);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Not a feature or missing Value Info");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: PREVIOUS_VALUE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {

                                                    pRowCh = null;
                                                    AAState.WriteLine("                  Finished: PREVIOUS_VALUE");
                                                }
                                                break;
                                            }



                                        case "ANGLE":

                                            try
                                            {
                                                AAState.WriteLine("                  Trying: ANGLE");
                                                if (inFeature != null)
                                                {
                                                    if (intFldIdxs.Count == 0)
                                                    {
                                                        AAState.WriteLine("                  Field not found");
                                                        continue;

                                                    }
                                                    if ((inFeature.Class as IFeatureClass).ShapeType != esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        AAState.WriteLine("                 Input feature is not a line");
                                                        continue;
                                                    }

                                                    bool boolGeo = true;
                                                    if (valData.Trim() != "")
                                                    {
                                                        if (valData.ToUpper() == "A")
                                                        {
                                                            boolGeo = false;
                                                        }
                                                    }
                                                    AAState.WriteLine("                 Getting Angle for feature");
                                                    IPolyline pLyLine = inFeature.Shape as IPolyline;

                                                    ILine pLine = new LineClass();
                                                    pLine.ToPoint = pLyLine.FromPoint;
                                                    pLine.FromPoint = pLyLine.ToPoint;


                                                    double angArth = pLine.Angle * 180 / Math.PI;
                                                    if (angArth < 0)
                                                    {
                                                        angArth = 360 + angArth;
                                                    }


                                                    double angGeo = 270 - angArth;
                                                    if (angGeo < 0)
                                                    {
                                                        angGeo = 360 + angGeo;
                                                    }
                                                    double ang;
                                                    if (boolGeo)
                                                    {
                                                        ang = angGeo;
                                                    }
                                                    else
                                                    {
                                                        ang = angArth;
                                                    }
                                                    AAState.WriteLine("                  Angle Calculated: " + ang);
                                                    try
                                                    {
                                                        inObject.set_Value(intFldIdxs[0], ang);
                                                        AAState.WriteLine("                  Angle Stored");

                                                    }
                                                    catch
                                                    {
                                                        AAState.WriteLine("                  Could not store angle");
                                                    }

                                                    pLine = null;
                                                    pLyLine = null;

                                                    AAState.WriteLine("                  Done");


                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: ANGLE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: ANGLE");
                                            }
                                            break;


                                        case "CREATE_PERP_LINE"://Layer to Search For|Offset Distante or Field|Search distance to look for a line|UseSnapPoint|TargetLayer|TargetLayerTemplate

                                            try
                                            {
                                                AAState.WriteLine("                  Trying: CREATE_PERP_LINE");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    double offsetVal = 5;
                                                    bool useSnapPnt = false;
                                                    string targetLayerName = "";
                                                    IFeatureLayer targetLayer = null;
                                                    string targetLayerTemp = "";

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    int fldOff = -1;
                                                    if (args.GetLength(0) == 6)
                                                    {

                                                        sourceLayerNames = args[0].ToString().Split(',');

                                                        if (Globals.IsNumeric(args[1]))
                                                            Double.TryParse(args[1], out offsetVal);
                                                        else
                                                        {
                                                            fldOff = Globals.GetFieldIndex(inObject.Fields, args[1]);

                                                        }

                                                        Double.TryParse(args[2], out searchDistance);
                                                        Boolean.TryParse(args[3], out useSnapPnt);
                                                        targetLayerName = args[4];
                                                        targetLayerTemp = args[5];

                                                    }
                                                    else if (args.GetLength(0) == 5)
                                                    {

                                                        sourceLayerNames = args[0].ToString().Split(',');

                                                        if (Globals.IsNumeric(args[1]))
                                                            Double.TryParse(args[1], out offsetVal);
                                                        else
                                                        {
                                                            fldOff = Globals.GetFieldIndex(inObject.Fields, args[1]);

                                                        }

                                                        Double.TryParse(args[2], out searchDistance);
                                                        Boolean.TryParse(args[3], out useSnapPnt);
                                                        targetLayerName = args[4];
                                                        targetLayerTemp = "";
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Incorrect Value info was not found");
                                                        continue;
                                                    }
                                                    if (intFldIdxs.Count > 0)
                                                    {
                                                        AAState.WriteLine("                  WARNING: Input fields are not used for this tool");
                                                        continue;
                                                    }

                                                    targetLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, targetLayerName, ref boolLayerOrFC);
                                                    if (targetLayer == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Target layer not found. " + targetLayerName);
                                                        continue;
                                                    }
                                                    if (targetLayer.FeatureClass.ShapeType != esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Target layer is not a line layer. " + targetLayerName);
                                                        continue;
                                                    }


                                                    for (int i = 0; i < sourceLayerNames.Length; i++)
                                                    {
                                                        sourceLayerName = sourceLayerNames[i].ToString();
                                                        if (sourceLayerName != "")

                                                            sourceLayerName = args[i].ToString();
                                                        if (i == 0)
                                                            i++;
                                                        boolLayerOrFC = true;

                                                        if (sourceLayerName.Contains("("))
                                                        {
                                                            string[] tempSplt = sourceLayerName.Split('(');
                                                            sourceLayerName = tempSplt[0];
                                                            sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                            if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                            {
                                                                boolLayerOrFC = true;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                            {
                                                                boolLayerOrFC = true;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                            {
                                                                boolLayerOrFC = false;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                            {
                                                                boolLayerOrFC = false;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                            {
                                                                boolLayerOrFC = false;
                                                            }
                                                            else
                                                            {
                                                                boolLayerOrFC = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                        }
                                                        if (sourceLayer == null)
                                                        {
                                                            AAState.WriteLine("                  ERROR/WARNING: " + sourceLayer + " was not found");
                                                            continue;
                                                        }
                                                        if (fldOff != -1)
                                                        {
                                                            try
                                                            {
                                                                string temp = inObject.get_Value(fldOff).ToString();
                                                                if (Globals.IsNumeric(temp))
                                                                {
                                                                    Double.TryParse(temp, out offsetVal);
                                                                }
                                                            }
                                                            catch
                                                            {
                                                            }

                                                        }

                                                        IPolyline pTempLine = new PolylineClass();
                                                        pTempLine = Globals.CreateAngledLineFromLocationOnLine((IPoint)inFeature.Shape, sourceLayer,
                                                            boolLayerOrFC, Globals.ConvertDegToRads(90), offsetVal, "true", true,false);

                                                        IEditTemplate pTemp = null;
                                                        IFeature pFeat = null;

                                                        if (targetLayerTemp != "")
                                                            pTemp = Globals.GetEditTemplate(targetLayerTemp, targetLayer);
                                                        if (pTemp != null)
                                                        {
                                                            AAState.WriteLine("                  Template found");
                                                            pFeat = Globals.CreateFeature(pTempLine, pTemp, AAState._editor, ArcMap.Application, false, false, false);

                                                        }
                                                        else
                                                        {

                                                            pFeat = Globals.CreateFeature(pTempLine, targetLayer, AAState._editor, ArcMap.Application, false, false, false);
                                                        }

                                                        if (NewFeatureList == null)
                                                        {
                                                            NewFeatureList = new List<IObject>();
                                                        }
                                                        AAState.WriteLine("                  Added to the new feature list");
                                                        NewFeatureList.Add(pFeat);

                                                    }
                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: CREATE_PERP_LINE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: CREATE_PERP_LINE");
                                            }
                                            break;
                                        case "AUTONUMBER"://Layer to Search For|Offset Distante or Field|Search distance to look for a line 

                                            try
                                            {
                                                AAState.WriteLine("                  Trying: AUTONUMBER");
                                                if (inObject != null)
                                                {
                                                    if (intFldIdxs.Count == 0)
                                                    {
                                                        AAState.WriteLine("                  Field not found");
                                                        continue;

                                                    }
                                                    AAState.WriteLine("                  Getting Max value for Field: " + strFldNames[0]);
                                                    string res = Globals.GetFieldStats(inObject.Class as IFeatureClass, strFldNames[0], Globals.statsType.Max);
                                                    AAState.WriteLine("                  Value returned: " + res);
                                                    if (res == "External component has thrown an exception.")
                                                    {
                                                        AAState.WriteLine("                  The field specified was not a numeric field");
                                                    }
                                                    else
                                                    {

                                                        if (Globals.IsNumeric(res))
                                                        {
                                                            AAState.WriteLine("                  Value is numeric");

                                                            AAState.WriteLine("                  Trying to Incriment " + res);
                                                            long val = (Convert.ToInt64(res) + 1);
                                                            AAState.WriteLine("                  Incrimented to " + res);
                                                            try
                                                            {
                                                                AAState.WriteLine("                  Trying to set value " + res);
                                                                inObject.set_Value(intFldIdxs[0], val);
                                                                AAState.WriteLine("                  Value set" + res);



                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                AAState.WriteLine("                  ERROR: Could not set value: " + ex.Message.ToString());
                                                            }


                                                        }


                                                        else
                                                        {
                                                            AAState.WriteLine("                  Value is not numeric: " + res);
                                                        }
                                                    }


                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: AUTONUMBER: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: AUTONUMBER");
                                            }
                                            break;



                                        case "COPY_LINKED_RECORD"://Feature Layer|Field To Copy|Primary Key Field|Foreign Key Field
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying: COPY_LINKED_RECORD");
                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 4)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    if (inObject == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: The input features is null");
                                                        break;
                                                    }
                                                    string[] targetLayerNames;
                                                    string targetFieldName = "";

                                                    found = false;
                                                    AAState.WriteLine("                  Getting Value Info");
                                                    targetLayerNames = args[0].ToString().Split(',');
                                                    targetFieldName = args[1].ToString();
                                                    string targetLayerName = "";
                                                    string sourceIDFieldName = args[2].ToString();
                                                    string targetIDFieldName = args[3].ToString();
                                                    AAState.WriteLine("                  Checking values");
                                                    if (targetFieldName != null)
                                                    {
                                                        AAState.WriteLine("                  Checking Field in Edited Layer");

                                                        int fldIDSourecIdx = Globals.GetFieldIndex(inObject.Fields, sourceIDFieldName);
                                                        if (fldIDSourecIdx > -1 && intFldIdxs.Count > 0)
                                                        {
                                                            if (inObject.get_Value(fldIDSourecIdx) != null && inObject.get_Value(fldIDSourecIdx) != DBNull.Value)
                                                            {
                                                                if (inObject.get_Value(fldIDSourecIdx).ToString() != "")
                                                                {


                                                                    for (int i = 0; i < targetLayerNames.Length; i++)
                                                                    {
                                                                        targetLayerName = targetLayerNames[i].ToString().Trim();

                                                                        if (targetLayerName != "")
                                                                        {

                                                                            // Get layer
                                                                            AAState.WriteLine("                  Checking for join record");
                                                                            bool FCorLayerSource = true;
                                                                            sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, targetLayerName, ref FCorLayerSource);

                                                                            if (sourceLayer != null)
                                                                            {

                                                                                int fldValToCopyIdx = sourceLayer.FeatureClass.Fields.FindField(targetFieldName);
                                                                                int fldIDTargetIdx = sourceLayer.FeatureClass.Fields.FindField(targetIDFieldName);
                                                                                if (fldIDTargetIdx > -1 && fldValToCopyIdx > -1)
                                                                                {
                                                                                    IQueryFilter pQFilt = Globals.createQueryFilter();
                                                                                    if (sourceLayer.FeatureClass.Fields.get_Field(fldIDTargetIdx).Type == esriFieldType.esriFieldTypeString)
                                                                                    {
                                                                                        pQFilt.WhereClause = "" + sourceLayer.FeatureClass.Fields.get_Field(fldIDTargetIdx).Name + "" + " = '" + inObject.get_Value(fldIDSourecIdx).ToString() + "'";

                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        pQFilt.WhereClause = sourceLayer.FeatureClass.Fields.get_Field(fldIDTargetIdx).Name + " = " + inObject.get_Value(fldIDSourecIdx);

                                                                                    }
                                                                                    IFeatureCursor pCurs;

                                                                                    pCurs = sourceLayer.FeatureClass.Search(pQFilt, true);
                                                                                    IFeature pRow;
                                                                                    while ((pRow = pCurs.NextFeature()) != null)
                                                                                    {
                                                                                        AAState.WriteLine("                  Trying to Copy Value from Record");
                                                                                        try
                                                                                        {
                                                                                            inObject.set_Value(intFldIdxs[0], pRow.get_Value(fldValToCopyIdx));
                                                                                            break;

                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                            AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(fldValToCopyIdx) + " to field: " + strFldNames[0]);
                                                                                        }

                                                                                        pRow = null;
                                                                                    }
                                                                                    pRow = null;

                                                                                    AAState.WriteLine("                  Value successfully copied");


                                                                                    if (pCurs != null)
                                                                                        Marshal.ReleaseComObject(pCurs);
                                                                                    pCurs = null;

                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  ERROR: ID or Field to populate was not found");
                                                                                }


                                                                            }
                                                                            else
                                                                            {

                                                                                ITable pTable = Globals.FindTable(AAState._editor.Map, targetLayerName);
                                                                                if (pTable != null)
                                                                                {
                                                                                    int fldValToCopyIdx = Globals.GetFieldIndex(pTable.Fields, targetFieldName);
                                                                                    int fldIDTargetIdx = Globals.GetFieldIndex(pTable.Fields, targetIDFieldName);
                                                                                    if (fldIDTargetIdx > -1 && fldValToCopyIdx > -1)
                                                                                    {
                                                                                        IQueryFilter pQFilt = Globals.createQueryFilter();
                                                                                        if (pTable.Fields.get_Field(fldIDTargetIdx).Type == esriFieldType.esriFieldTypeString)
                                                                                        {
                                                                                            pQFilt.WhereClause = "" + pTable.Fields.get_Field(fldIDTargetIdx).Name + "" + " = '" + inObject.get_Value(fldIDSourecIdx).ToString() + "'";

                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            pQFilt.WhereClause = pTable.Fields.get_Field(fldIDTargetIdx).Name + " = " + inObject.get_Value(fldIDSourecIdx);

                                                                                        }
                                                                                        ICursor pCurs;

                                                                                        pCurs = pTable.Search(pQFilt, true);
                                                                                        IRow pRow;
                                                                                        bool valSet = false;
                                                                                        pRow = pCurs.NextRow();
                                                                                        while (pRow != null)
                                                                                        {
                                                                                            AAState.WriteLine("                  Trying to Copy Value from Record");
                                                                                            try
                                                                                            {
                                                                                                inObject.set_Value(intFldIdxs[0], pRow.get_Value(fldValToCopyIdx));
                                                                                                AAState.WriteLine("                  " + pRow.get_Value(fldValToCopyIdx).ToString() + " Set in related record");
                                                                                                valSet = true;
                                                                                                break;

                                                                                            }
                                                                                            catch
                                                                                            {
                                                                                                AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(fldValToCopyIdx) + " to field: " + strFldNames[0]);
                                                                                            }

                                                                                            pRow = pCurs.NextRow();

                                                                                        }
                                                                                        pRow = null;
                                                                                        if (valSet)
                                                                                            AAState.WriteLine("                  Value successfully copied");
                                                                                        else
                                                                                            AAState.WriteLine("                  Related record not found.");

                                                                                        if (pCurs != null)
                                                                                            Marshal.ReleaseComObject(pCurs);
                                                                                        pCurs = null;

                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR: ID or Field to populate was not found");
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  ERROR: Table to populate not found: " + sourceLayerName);
                                                                                }
                                                                                pTable = null;

                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: ID was null");
                                                        }


                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: COPY_LINKED_RECORD" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: COPY_LINKED_RECORD");


                                                }
                                                break;

                                            }
                                        case "UPDATE_LINKED_RECORD"://Feature Layer|Field To Copy|Primary Key Field|Foreign Key Field
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying: UPDATE_LINKED_RECORD");
                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 4)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    if (inObject == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: The input features is null");
                                                        break;
                                                    }



                                                    IRowChanges pRowCh = inObject as IRowChanges;

                                                    if (pRowCh.get_ValueChanged(intFldIdxs[0]) == false)
                                                    {
                                                        AAState.WriteLine("                  PROMPT: Target value did not change, skipping");
                                                        pRowCh = null;
                                                        continue;
                                                    }
                                                    else
                                                    {
                                                        pRowCh = null;
                                                        string[] targetLayerNames;
                                                        string targetFieldName = "";

                                                        found = false;
                                                        AAState.WriteLine("                  Getting Value Info");
                                                        targetLayerNames = args[0].ToString().Split(',');
                                                        targetFieldName = args[1].ToString();
                                                        string targetLayerName = "";
                                                        string sourceIDFieldName = args[2].ToString();
                                                        string targetIDFieldName = args[3].ToString();
                                                        AAState.WriteLine("                  Checking values");
                                                        if (targetFieldName != null)
                                                        {
                                                            AAState.WriteLine("                  Checking Field in Edited Layer");

                                                            int fldIDSourecIdx = Globals.GetFieldIndex(inObject.Fields, sourceIDFieldName);
                                                            if (fldIDSourecIdx > -1 && intFldIdxs.Count > 0)
                                                            {
                                                                if (inObject.get_Value(fldIDSourecIdx) != null && inObject.get_Value(fldIDSourecIdx) != DBNull.Value)
                                                                {
                                                                    if (inObject.get_Value(fldIDSourecIdx).ToString() != "")
                                                                    {


                                                                        for (int i = 0; i < targetLayerNames.Length; i++)
                                                                        {
                                                                            targetLayerName = targetLayerNames[i].ToString().Trim();

                                                                            if (targetLayerName != "")
                                                                            {

                                                                                // Get layer
                                                                                AAState.WriteLine("                  Checking for related record");
                                                                                bool FCorLayerSource = true;
                                                                                sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, targetLayerName, ref FCorLayerSource);

                                                                                if (sourceLayer != null)
                                                                                {

                                                                                    int fldValToCopyIdx = sourceLayer.FeatureClass.Fields.FindField(targetFieldName);
                                                                                    int fldIDTargetIdx = sourceLayer.FeatureClass.Fields.FindField(targetIDFieldName);
                                                                                    if (fldIDTargetIdx > -1 && fldValToCopyIdx > -1)
                                                                                    {
                                                                                        IQueryFilter pQFilt = Globals.createQueryFilter();
                                                                                        if (sourceLayer.FeatureClass.Fields.get_Field(fldIDTargetIdx).Type == esriFieldType.esriFieldTypeString)
                                                                                        {
                                                                                            pQFilt.WhereClause = "" + sourceLayer.FeatureClass.Fields.get_Field(fldIDTargetIdx).Name + "" + " = '" + inObject.get_Value(fldIDSourecIdx).ToString() + "'";

                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            pQFilt.WhereClause = sourceLayer.FeatureClass.Fields.get_Field(fldIDTargetIdx).Name + " = " + inObject.get_Value(fldIDSourecIdx);

                                                                                        }
                                                                                        IFeatureCursor pCurs;

                                                                                        pCurs = sourceLayer.FeatureClass.Search(pQFilt, true);
                                                                                        IFeature pRow;
                                                                                        bool valSet = false;
                                                                                        while ((pRow = pCurs.NextFeature()) != null)
                                                                                        {
                                                                                            AAState.WriteLine("                  Trying to Copy Value to the Record");
                                                                                            try
                                                                                            {
                                                                                                pRow.set_Value(fldValToCopyIdx, inObject.get_Value(intFldIdxs[0]));
                                                                                                pRow.Store();
                                                                                                AAState.WriteLine("                  " + inObject.get_Value(intFldIdxs[0]).ToString() + " Set in related record");
                                                                                                valSet = true;


                                                                                            }
                                                                                            catch
                                                                                            {
                                                                                                AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(intFldIdxs[0]).ToString() + " to field: " + targetFieldName);
                                                                                            }

                                                                                            pRow = null;
                                                                                        }
                                                                                        pRow = null;

                                                                                        if (valSet)
                                                                                            AAState.WriteLine("                  Value successfully copied");
                                                                                        else
                                                                                            AAState.WriteLine("                  Related record not found.");

                                                                                        if (pCurs != null)
                                                                                            Marshal.ReleaseComObject(pCurs);
                                                                                        pCurs = null;

                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR: ID or Field to populate was not found");
                                                                                    }


                                                                                }
                                                                                else
                                                                                {

                                                                                    ITable pTable = Globals.FindTable(AAState._editor.Map, targetLayerName);
                                                                                    if (pTable != null)
                                                                                    {
                                                                                        int fldValToCopyIdx = Globals.GetFieldIndex(pTable.Fields, targetFieldName);
                                                                                        int fldIDTargetIdx = Globals.GetFieldIndex(pTable.Fields, targetIDFieldName);
                                                                                        if (fldIDTargetIdx > -1 && fldValToCopyIdx > -1)
                                                                                        {
                                                                                            IQueryFilter pQFilt = Globals.createQueryFilter();
                                                                                            if (pTable.Fields.get_Field(fldIDTargetIdx).Type == esriFieldType.esriFieldTypeString)
                                                                                            {
                                                                                                pQFilt.WhereClause = "" + pTable.Fields.get_Field(fldIDTargetIdx).Name + "" + " = '" + inObject.get_Value(fldIDSourecIdx).ToString() + "'";

                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                pQFilt.WhereClause = pTable.Fields.get_Field(fldIDTargetIdx).Name + " = " + inObject.get_Value(fldIDSourecIdx);

                                                                                            }
                                                                                            ICursor pCurs;

                                                                                            pCurs = pTable.Search(pQFilt, false);
                                                                                            IRow pRow;
                                                                                            bool valSet = false;
                                                                                            pRow = pCurs.NextRow();
                                                                                            while (pRow != null)
                                                                                            {
                                                                                                AAState.WriteLine("                  Trying to Copy Value from Record");
                                                                                                try
                                                                                                {
                                                                                                    pRow.set_Value(fldValToCopyIdx, inObject.get_Value(intFldIdxs[0]));
                                                                                                    pRow.Store();

                                                                                                    AAState.WriteLine("                  " + inObject.get_Value(intFldIdxs[0]).ToString() + " Set in related record");
                                                                                                    valSet = true;


                                                                                                }
                                                                                                catch
                                                                                                {
                                                                                                    AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(fldValToCopyIdx) + " to field: " + strFldNames[0]);
                                                                                                }

                                                                                                pRow = pCurs.NextRow();

                                                                                            }
                                                                                            pRow = null;
                                                                                            if (valSet)
                                                                                                AAState.WriteLine("                  Value successfully copied");
                                                                                            else
                                                                                                AAState.WriteLine("                  Related record not found.");

                                                                                            if (pCurs != null)
                                                                                                Marshal.ReleaseComObject(pCurs);
                                                                                            pCurs = null;

                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            AAState.WriteLine("                  ERROR: ID or Field to populate was not found");
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR: Table to populate not found: " + sourceLayerName);
                                                                                    }
                                                                                    pTable = null;

                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: ID or Field to Copy was not found");
                                                            }


                                                        }

                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: UPDATE_LINKED_RECORD" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: UPDATE_LINKED_RECORD");


                                                }
                                                break;

                                            }

                                        case "OFFSET"://Layer to Search For|Offset Distante or Field|Search distance to look for a line 

                                            try
                                            {
                                                AAState.WriteLine("                  Trying: OFFSET");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    double offsetVal = 5;


                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    int fldOff = -1;
                                                    if (args.GetLength(0) >= 3)
                                                    {

                                                        sourceLayerNames = args[0].ToString().Split(',');

                                                        if (Globals.IsNumeric(args[1]))
                                                            Double.TryParse(args[1], out offsetVal);
                                                        else
                                                        {
                                                            fldOff = Globals.GetFieldIndex(inObject.Fields, args[1]);

                                                        }

                                                        Double.TryParse(args[2], out searchDistance);
                                                    }
                                                    else if (args.GetLength(0) >= 2)
                                                    {

                                                        sourceLayerNames = args[0].ToString().Split(',');

                                                        if (Globals.IsNumeric(args[1]))
                                                            Double.TryParse(args[1], out offsetVal);
                                                        else
                                                        {
                                                            fldOff = Globals.GetFieldIndex(inObject.Fields, args[1]);

                                                        }

                                                        Double.TryParse("1", out searchDistance);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Incorrect Value info was not found");
                                                        continue;
                                                    }
                                                    if (intFldIdxs.Count != 2)
                                                    {
                                                        AAState.WriteLine("                  ERROR: 2 fields in fieldname are required for this tool");
                                                        continue;
                                                    }

                                                    // Get layer


                                                    for (int i = 0; i < sourceLayerNames.Length; i++)
                                                    {
                                                        sourceLayerName = sourceLayerNames[i].ToString();
                                                        if (sourceLayerName != "")

                                                            sourceLayerName = args[i].ToString();
                                                        if (i == 0)
                                                            i++;
                                                        boolLayerOrFC = true;

                                                        if (sourceLayerName.Contains("("))
                                                        {
                                                            string[] tempSplt = sourceLayerName.Split('(');
                                                            sourceLayerName = tempSplt[0];
                                                            sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                            if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                            {
                                                                boolLayerOrFC = true;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                            {
                                                                boolLayerOrFC = true;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                            {
                                                                boolLayerOrFC = false;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                            {
                                                                boolLayerOrFC = false;
                                                            }
                                                            else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                            {
                                                                boolLayerOrFC = false;
                                                            }
                                                            else
                                                            {
                                                                boolLayerOrFC = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                        }
                                                        if (sourceLayer == null)
                                                        {
                                                            AAState.WriteLine("                  ERROR/WARNING: " + sourceLayer + " was not found");
                                                            continue;
                                                        }

                                                        IFeatureClass iFC = inFeature.Class as IFeatureClass;
                                                        if (sourceLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                                                        {
                                                            AAState.WriteLine("                  ERROR: " + sourceLayer + " is a polygon layer");

                                                            break;
                                                        }
                                                        if (sourceLayer != null)
                                                        {

                                                            sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, searchDistance, false);

                                                            pFS = (IFeatureSelection)sourceLayer;
                                                            if (boolLayerOrFC)
                                                            {
                                                                if (pFS.SelectionSet.Count > 0)
                                                                {
                                                                    pFS.SelectionSet.Search(sFilter, true, out cCurs);
                                                                    fCursor = cCurs as IFeatureCursor;


                                                                }
                                                                else
                                                                {
                                                                    fCursor = sourceLayer.Search(sFilter, true);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                            }

                                                            while ((sourceFeature = fCursor.NextFeature()) != null)
                                                            {
                                                                double dAlong = 0;
                                                                if (sourceFeature.Class != inFeature.Class)
                                                                {
                                                                    IPoint pIntPnt;
                                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                                    {
                                                                        pIntPnt = Globals.GetIntersection(inFeature.ShapeCopy, sourceFeature.ShapeCopy as IPolyline) as IPoint;

                                                                    }
                                                                    else
                                                                        pIntPnt = inFeature.ShapeCopy as IPoint;
                                                                    IPoint snapPnt = null;

                                                                    dAlong = Globals.PointDistanceOnLine(pIntPnt, sourceFeature.Shape as IPolyline, 2, out snapPnt);

                                                                    snapPnt = null;
                                                                    pIntPnt = null;



                                                                }

                                                                else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                                {
                                                                    IPoint pIntPnt;
                                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                                    {
                                                                        pIntPnt = Globals.GetIntersection(inFeature.ShapeCopy, sourceFeature.ShapeCopy as IPolyline) as IPoint;

                                                                    }
                                                                    else
                                                                        pIntPnt = inFeature.ShapeCopy as IPoint;
                                                                    IPoint snapPnt = null;

                                                                    dAlong = Globals.PointDistanceOnLine(pIntPnt, sourceFeature.Shape as IPolyline, 2, out snapPnt);
                                                                    snapPnt = null;

                                                                    pIntPnt = null;

                                                                }
                                                                AAState.WriteLine("                  Distance found: " + dAlong);
                                                                IPoint pNewPt = new PointClass();
                                                                IConstructPoint2 pConsPoint = pNewPt as IConstructPoint2;

                                                                if (fldOff != -1)
                                                                {
                                                                    string temp = inObject.get_Value(fldOff).ToString();
                                                                    if (Globals.IsNumeric(temp))
                                                                    {
                                                                        Double.TryParse(temp, out offsetVal);
                                                                    }

                                                                }
                                                                pConsPoint.ConstructOffset
                                                                    (sourceFeature.Shape as ICurve, esriSegmentExtension.esriNoExtension, dAlong, false, offsetVal);

                                                                inObject.set_Value(intFldIdxs[0], pNewPt.X);
                                                                inObject.set_Value(intFldIdxs[1], pNewPt.Y);

                                                                pNewPt = null;
                                                                pConsPoint = null;
                                                            }

                                                        }

                                                    }
                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: OFFSET: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: OFFSET");
                                            }
                                            break;




                                        case "SIDE":
                                            {
                                                AAState.WriteLine("                  Trying: SIDE");


                                                try
                                                {
                                                    //Layer|IDField|IDField source

                                                    if ((valData != null) && (inFeature != null))
                                                    {
                                                        AAState.WriteLine("                     Feature and valueinfo is valid");



                                                        AAState.WriteLine("                     Splitting up value info: " + valData);
                                                        sourceLayerName = "";
                                                        sourceFieldName = "";
                                                        sourceField = -1;
                                                        string inputFieldName = "";
                                                        args = valData.Split('|');
                                                        if (args.Length < 2)
                                                        {
                                                            AAState.WriteLine("                  ERROR: SIDE: Value info does not have enough parameters");
                                                            continue;
                                                        }


                                                        switch (args.Length)
                                                        {
                                                            case 3:
                                                                sourceLayerNames = args[0].ToString().Split(',');
                                                                sourceFieldName = args[1].ToString();
                                                                inputFieldName = args[2].ToString();
                                                                break;
                                                            default:
                                                                AAState.WriteLine("                  ERROR: SIDE: Value info does not have enough parameters");
                                                                continue;

                                                        }
                                                        int fldValToCopyIdx = inObject.Fields.FindField(inputFieldName);

                                                        if (fldValToCopyIdx > -1)
                                                        {

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                sourceLayerName = sourceLayerNames[i].ToString();

                                                                if (sourceLayerName != "")
                                                                {

                                                                    boolLayerOrFC = true;
                                                                    if (sourceLayerName.Contains("("))
                                                                    {
                                                                        string[] tempSplt = sourceLayerName.Split('(');
                                                                        sourceLayerName = tempSplt[0];
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                    }
                                                                    if (sourceLayer != null)
                                                                    {


                                                                        // Get layer
                                                                        AAState.WriteLine("                  " + sourceLayerName + " Layer found");
                                                                        int fldValTargetJoinIdx = Globals.GetFieldIndex(sourceLayer, sourceFieldName);
                                                                        if (fldValTargetJoinIdx > -1)
                                                                        {
                                                                            IQueryFilter pQFilt = Globals.createQueryFilter();

                                                                            if (sourceLayer.FeatureClass.Fields.get_Field(fldValTargetJoinIdx).Type == esriFieldType.esriFieldTypeString)
                                                                            {
                                                                                pQFilt.WhereClause = "" + sourceLayer.FeatureClass.Fields.get_Field(fldValTargetJoinIdx).Name + "" + " = '" + inObject.get_Value(fldValToCopyIdx).ToString() + "'";

                                                                            }
                                                                            else
                                                                            {
                                                                                pQFilt.WhereClause = sourceLayer.FeatureClass.Fields.get_Field(fldValTargetJoinIdx).Name + " = " + inObject.get_Value(fldValToCopyIdx);

                                                                            }
                                                                            AAState.WriteLine("                     Where Clause: " + pQFilt.WhereClause);
                                                                            int cnt = sourceLayer.FeatureClass.FeatureCount(pQFilt);
                                                                            AAState.WriteLine("                     Feature Found: " + cnt);
                                                                            if (cnt > 0)
                                                                            {

                                                                                fCursor = sourceLayer.FeatureClass.Search(pQFilt, true);
                                                                                while ((sourceFeature = fCursor.NextFeature()) != null)
                                                                                {
                                                                                    bool side = false;
                                                                                    if (Globals.GetPointOnLine(inFeature.Shape, sourceFeature.Shape, 450, out side) != null)
                                                                                    {

                                                                                        if (side)
                                                                                        {
                                                                                            AAState.WriteLine("                     Right Side");

                                                                                            inFeature.set_Value(intFldIdxs[0], "Right");
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            AAState.WriteLine("                     Left Side");
                                                                                            inFeature.set_Value(intFldIdxs[0], "Left");
                                                                                        }
                                                                                        if (fCursor != null)
                                                                                            Marshal.ReleaseComObject(fCursor);
                                                                                        fCursor = null;
                                                                                        continue;
                                                                                    }

                                                                                }
                                                                                if (fCursor != null)
                                                                                    Marshal.ReleaseComObject(fCursor);
                                                                                fCursor = null;
                                                                            }
                                                                            pQFilt = null;

                                                                        }

                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: SIDE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    if (fCursor != null)
                                                        Marshal.ReleaseComObject(fCursor);
                                                    fCursor = null;

                                                    AAState.WriteLine("                  Finished: SIDE");

                                                }
                                                break;

                                            }



                                        case "PROMPT":
                                            {
                                                //Loop through all fields list in the fieldname
                                                //If blank or null, prompt user for value
                                                //Store Value

                                                IDomain pDom = default(IDomain);
                                                ISubtypes pSubType = null;
                                                List<Globals.DomSubList> lst = null;

                                                Globals.DomSubList dmRetVal = null;

                                                try
                                                {
                                                    if ((inObject != null))
                                                    {


                                                        pSubType = (ISubtypes)inObject.Class;


                                                        if (pSubType.HasSubtype)
                                                        {
                                                            int intSub;
                                                            if (intFldIdxs.Contains(pSubType.SubtypeFieldIndex))
                                                            {
                                                                lst = Globals.SubtypeToList(pSubType);
                                                                if (inObject.get_Value(pSubType.SubtypeFieldIndex) == null || inObject.get_Value(pSubType.SubtypeFieldIndex) == "" || inObject.get_Value(pSubType.SubtypeFieldIndex) == DBNull.Value)
                                                                {
                                                                    dmRetVal = Globals.showValuesOptionsForm(lst, "Provide a value for " + inObject.Class.AliasName + ":" + pSubType.SubtypeFieldName, "Provide a value for " + inObject.Class.AliasName + ":" + pSubType.SubtypeFieldName, ComboBoxStyle.DropDownList);
                                                                    inObject.set_Value(pSubType.SubtypeFieldIndex, dmRetVal.Value);

                                                                    intSub = Convert.ToInt32(dmRetVal.Value);
                                                                }
                                                                else
                                                                {
                                                                    intSub = Convert.ToInt32(inObject.get_Value(pSubType.SubtypeFieldIndex));
                                                                }

                                                                for (int l = 0; l < strFldNames.Count; l++)
                                                                {
                                                                    if (intFldIdxs[l] == pSubType.SubtypeFieldIndex)
                                                                        continue;

                                                                    if (intFldIdxs[l] != -1)
                                                                    {
                                                                        pDom = pSubType.get_Domain(intSub, inObject.Fields.get_Field(intFldIdxs[l]).Name);

                                                                        if (pDom == null)
                                                                        {
                                                                            if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                            {
                                                                                IList<string> pVals = new List<string>();

                                                                                string strRetVal = Globals.showValuesOptionsForm(pVals, "Provide a value for " + inObject.Class.AliasName + ":" + strFldAlias[l], "Provide a value for " + inObject.Class.AliasName + ":" + strFldAlias[l], ComboBoxStyle.DropDown);

                                                                                try
                                                                                {
                                                                                    inObject.set_Value(intFldIdxs[l], strRetVal);
                                                                                }
                                                                                catch
                                                                                {

                                                                                }
                                                                                pVals = null;
                                                                            }



                                                                        }
                                                                        else
                                                                        {

                                                                            if (pDom is CodedValueDomain)
                                                                            {

                                                                                lst = Globals.DomainToList(pDom);
                                                                                if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                                {

                                                                                    dmRetVal = Globals.showValuesOptionsForm(lst, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDownList);
                                                                                    try
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[l], dmRetVal.Value);
                                                                                    }
                                                                                    catch
                                                                                    {

                                                                                    }

                                                                                    lst = null;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                                {

                                                                                    IList<string> pVals = new List<string>();
                                                                                    string strRetVal = Globals.showValuesOptionsForm(pVals, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDown);

                                                                                    try
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[l], strRetVal);
                                                                                    }
                                                                                    catch
                                                                                    {

                                                                                    }


                                                                                    pVals = null;
                                                                                }
                                                                            }

                                                                        }



                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  PROMPT: " + strFldNames[l] + " was not found");
                                                                    }
                                                                }

                                                            }
                                                            else
                                                            {
                                                                if (inObject.get_Value(pSubType.SubtypeFieldIndex) == null)
                                                                    intSub = pSubType.DefaultSubtypeCode;
                                                                else

                                                                    intSub = Convert.ToInt32(inObject.get_Value(pSubType.SubtypeFieldIndex));
                                                                for (int l = 0; l < strFldNames.Count; l++)
                                                                {
                                                                    if (intFldIdxs[l] != -1)
                                                                    {
                                                                        pDom = pSubType.get_Domain(intSub, inObject.Fields.get_Field(intFldIdxs[l]).Name);

                                                                        if (pDom == null)
                                                                        {
                                                                            if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                            {

                                                                                IList<string> pVals = new List<string>();
                                                                                string strRetVal = Globals.showValuesOptionsForm(pVals, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDown);

                                                                                try
                                                                                {
                                                                                    inObject.set_Value(intFldIdxs[l], strRetVal);
                                                                                }
                                                                                catch
                                                                                {

                                                                                }

                                                                                pVals = null;
                                                                            }
                                                                        }
                                                                        else
                                                                        {

                                                                            if (pDom is CodedValueDomain)
                                                                            {
                                                                                if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                                {


                                                                                    lst = Globals.DomainToList(pDom);

                                                                                    dmRetVal = Globals.showValuesOptionsForm(lst, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDownList);
                                                                                    try
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[l], dmRetVal.Value);
                                                                                    }
                                                                                    catch
                                                                                    {

                                                                                    }

                                                                                    lst = null;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                                {

                                                                                    IList<string> pVals = new List<string>();
                                                                                    string strRetVal = Globals.showValuesOptionsForm(pVals, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDown);

                                                                                    try
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[l], strRetVal);
                                                                                    }
                                                                                    catch
                                                                                    {

                                                                                    }

                                                                                    pVals = null;
                                                                                }
                                                                            }

                                                                        }



                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  PROMPT: " + strFldNames[l] + " was not found");
                                                                    }
                                                                }


                                                            }
                                                        }
                                                        else
                                                        {
                                                            for (int l = 0; l < strFldNames.Count; l++)
                                                            {

                                                                if (intFldIdxs[l] != -1)
                                                                {

                                                                    pDom = inObject.Fields.get_Field(intFldIdxs[l]).Domain;
                                                                    if (pDom == null)
                                                                    {
                                                                        if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                        {

                                                                            IList<string> pVals = new List<string>();
                                                                            string strRetVal = Globals.showValuesOptionsForm(pVals, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDown);
                                                                            try
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[l], strRetVal);
                                                                            }
                                                                            catch
                                                                            {

                                                                            }

                                                                            pVals = null;
                                                                        }
                                                                    }
                                                                    else
                                                                    {

                                                                        if (pDom is CodedValueDomain)
                                                                        {
                                                                            if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                            {


                                                                                lst = Globals.DomainToList(pDom);

                                                                                dmRetVal = Globals.showValuesOptionsForm(lst, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDownList);
                                                                                try
                                                                                {
                                                                                    inObject.set_Value(intFldIdxs[l], dmRetVal.Value);
                                                                                }
                                                                                catch
                                                                                {

                                                                                }

                                                                                lst = null;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (inObject.get_Value(intFldIdxs[l]) == null || inObject.get_Value(intFldIdxs[l]) == "" || inObject.get_Value(intFldIdxs[l]) == DBNull.Value)
                                                                            {

                                                                                IList<string> pVals = new List<string>();
                                                                                string strRetVal = Globals.showValuesOptionsForm(pVals, "Provide a value for " + strFldAlias[l], "Provide a value for " + strFldAlias[l], ComboBoxStyle.DropDown);
                                                                                try
                                                                                {
                                                                                    inObject.set_Value(intFldIdxs[l], strRetVal);
                                                                                }
                                                                                catch
                                                                                {

                                                                                }
                                                                                pVals = null;
                                                                            }
                                                                        }

                                                                    }



                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  PROMPT: " + strFldNames[l] + " was not found");
                                                                }
                                                            }
                                                        }



                                                    }
                                                }

                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: PROMPT" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {

                                                    AAState.WriteLine("                  Finished: PROMPT");
                                                    pDom = null;
                                                    pSubType = null;
                                                    lst = null;

                                                    dmRetVal = null;
                                                }
                                                break;
                                            }

                                        case "CASCADE_ATTRIBUTE":
                                            {
                                                AAState.WriteLine("                  Trying: CASCADE_ATTRIBUTE");

                                                string flds;
                                                string targetLayer;
                                                IRowChanges pRowCh = null;

                                                try
                                                {


                                                    if ((valData != null) && (inObject != null))
                                                    {
                                                        AAState.WriteLine("                     Feature and valueinfo is valid");



                                                        AAState.WriteLine("                     Splitting up value info: " + valData);
                                                        //field name is the field to Check
                                                        //value|Layer|tempalte|Cut or Copy|field-toField
                                                        args = valData.Split('|');
                                                        if (args.Length < 2)
                                                        {
                                                            AAState.WriteLine("                  ERROR: CASCADE_ATTRIBUTE: Value info does not have enough parameters");
                                                            continue;
                                                        }

                                                        targetLayer = args[0];
                                                        flds = args[1];
                                                        bool bPrompt;
                                                        if (args.Length == 3)
                                                        {
                                                            if (args[2].ToUpper() == "T" || args[2].ToUpper() == "TRUE" || args[2].ToUpper() == "YES")
                                                            {
                                                                bPrompt = true;
                                                            }
                                                            else
                                                            {
                                                                bPrompt = false;
                                                            }

                                                        }
                                                        else
                                                        {
                                                            bPrompt = true;
                                                        }
                                                        pRowCh = inObject as IRowChanges;
                                                        if (pRowCh.get_ValueChanged(intFldIdxs[0]) == false)
                                                        {
                                                            AAState.WriteLine("                  CASCADE_ATTRIBUTE: Target value did not change, skipping");
                                                            continue;
                                                        }

                                                        bool boolFoundAsLayer = true;

                                                        sourceLayer = Globals.FindLayer(ArcMap.Application, args[0].ToString(), ref boolFoundAsLayer) as IFeatureLayer;
                                                        if (sourceLayer == null)
                                                        {
                                                            AAState.WriteLine("                  ERROR: CASCADE_ATTRIBUTE: Target layer was not found");
                                                            continue;
                                                        }
                                                        int intTargFld = -1;
                                                        intTargFld = sourceLayer.FeatureClass.Fields.FindField(flds);
                                                        if (intTargFld == -1)
                                                        {
                                                            intTargFld = sourceLayer.FeatureClass.Fields.FindFieldByAliasName(flds);
                                                            if (intTargFld != -1)
                                                            {
                                                                flds = sourceLayer.FeatureClass.Fields.get_Field(intTargFld).Name;

                                                            }
                                                        }
                                                        if (intTargFld > -1)
                                                        {

                                                            if (pRowCh.get_OriginalValue(intFldIdxs[0]).ToString().Trim() == "")
                                                                continue;
                                                            IQueryFilter pQFilt = new QueryFilterClass();
                                                            if (sourceLayer.FeatureClass.Fields.get_Field(intTargFld).Type == esriFieldType.esriFieldTypeString)
                                                            {
                                                                pQFilt.WhereClause = flds + " = '" + pRowCh.get_OriginalValue(intFldIdxs[0]) + "'";

                                                            }
                                                            else
                                                            {
                                                                pQFilt.WhereClause = flds + " = " + pRowCh.get_OriginalValue(intFldIdxs[0]) + "";

                                                            }

                                                            int featCnt = sourceLayer.FeatureClass.FeatureCount(pQFilt);
                                                            if (featCnt == 0)
                                                            {
                                                                AAState.WriteLine("                  Skipping, no Matching records found");

                                                            }
                                                            else
                                                            {
                                                                string promptLayname;

                                                                promptLayname = Globals.getClassName(sourceLayer);

                                                                if (bPrompt)
                                                                {
                                                                    if (MessageBox.Show("You are about to change " + featCnt + " rows in the " + promptLayname + " Feature Class, proceed?", "Cascade", MessageBoxButtons.YesNo) == DialogResult.Yes)
                                                                    {
                                                                        AAState.WriteLine("                  User accepted prompt");

                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  User declined prompt");

                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  Prompt surpressed");
                                                                }
                                                            }

                                                            IFeatureCursor pCalcCursor = sourceLayer.FeatureClass.Update(pQFilt, false);
                                                            IFeature updateFeat;
                                                            if (ChangeFeatureList == null)
                                                            {
                                                                ChangeFeatureList = new List<IObject>();
                                                            }
                                                            while ((updateFeat = pCalcCursor.NextFeature()) != null)
                                                            {

                                                                updateFeat.set_Value(intTargFld, inObject.get_Value(intFldIdxs[0]));
                                                                ChangeFeatureList.Add(updateFeat);

                                                                if (!trackCancel.Continue())
                                                                {
                                                                    AAState.WriteLine("                     Abort Edit");
                                                                    AAState._editor.AbortOperation();
                                                                    return false;
                                                                }
                                                            }
                                                            updateFeat = null;

                                                            if (pCalcCursor != null)
                                                            {
                                                                Marshal.ReleaseComObject(pCalcCursor);
                                                            }
                                                            pCalcCursor = null;

                                                            pQFilt = null;

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: CASCADE_ATTRIBUTE: Field was not found");
                                                        }

                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: CASCADE_ATTRIBUTE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    sourceLayer = null;

                                                    pRowCh = null;
                                                    AAState.WriteLine("                  Finished: CASCADE_ATTRIBUTE");
                                                }
                                                break;
                                            }

                                        case "COPY_FEATURE":
                                            {
                                                AAState.WriteLine("                  Trying: COPY_FEATURE");
                                                IFeatureLayer pTargetFL;
                                                string[] FldPairs;
                                                string targetValue;
                                                IRowChanges pRowCh = null;
                                                IFeature pNewFeat = null;
                                                try
                                                {


                                                    if ((valData != null) && (inFeature != null))
                                                    {
                                                        AAState.WriteLine("                     Feature and valueinfo is valid");



                                                        AAState.WriteLine("                     Splitting up value info: " + valData);
                                                        //field name is the field to Check
                                                        //value|Layer|tempalte|Cut or Copy|field-toField
                                                        args = valData.Split('|');
                                                        if (args.Length < 2)
                                                        {
                                                            AAState.WriteLine("                  ERROR: COPY_FEATURE: Value info does not have enough parameters");
                                                            continue;
                                                        }

                                                        targetValue = args[0];

                                                        pRowCh = inObject as IRowChanges;
                                                        if (pRowCh.get_ValueChanged(intFldIdxs[0]) == false)
                                                        {
                                                            AAState.WriteLine("                  COPY_FEATURE: Field listed in the Field Name did not change, skipping");
                                                            continue;
                                                        }
                                                        if (inFeature.get_Value(intFldIdxs[0]).ToString() != targetValue.ToString())
                                                        {
                                                            AAState.WriteLine("                  COPY_FEATURE: Target value did not match listed value, skipping");
                                                            continue;
                                                        }

                                                        bool FCorLayerTarget = true;

                                                        pTargetFL = Globals.FindLayer(ArcMap.Application, args[1].ToString(), ref FCorLayerTarget) as IFeatureLayer;
                                                        if (pTargetFL == null)
                                                        {
                                                            AAState.WriteLine("                  ERROR: COPY_FEATURE: Target layer was not found");
                                                            continue;
                                                        }
                                                        if (Globals.IsEditable(ref pTargetFL, ref AAState._editor) == false)
                                                        {
                                                            AAState.WriteLine("                  ERROR: COPY_FEATURE: Target layer is not editable");
                                                            continue;
                                                        }



                                                        if (pTargetFL.FeatureClass.ShapeType != (inFeature.Class as IFeatureClass).ShapeType)
                                                        {
                                                            AAState.WriteLine("                  ERROR: COPY_FEATURE: Target layer and Source layer are different geometry types");
                                                            continue;
                                                        }

                                                        FldPairs = null;
                                                        //value|Layer|tempalte|Cut or Copy|field-toField

                                                        IEditTemplate pEditTemp = null;
                                                        string sourceAction = "COPY";
                                                        string fldMatching = null;

                                                        switch (args.Length)
                                                        {
                                                            //case 2:


                                                            //    break;
                                                            case 3:
                                                                if (args[2].Trim() != "")
                                                                {
                                                                    pEditTemp = Globals.PromptAndGetEditTemplateGraphic(pTargetFL, args[2].Trim());

                                                                }
                                                                else
                                                                {
                                                                    pEditTemp = null;
                                                                }
                                                                break;
                                                            case 4:
                                                                if (args[2].Trim() != "")
                                                                {

                                                                    pEditTemp = Globals.PromptAndGetEditTemplateGraphic(pTargetFL, args[2].Trim());
                                                                }
                                                                else
                                                                {
                                                                    pEditTemp = null;
                                                                }
                                                                sourceAction = args[3].ToUpper().Trim();

                                                                break;
                                                            case 5:
                                                                if (args[2].Trim() != "")
                                                                {

                                                                    pEditTemp = Globals.PromptAndGetEditTemplateGraphic(pTargetFL, args[2].Trim());
                                                                }
                                                                else
                                                                {
                                                                    pEditTemp = null;
                                                                }
                                                                sourceAction = args[3].ToUpper().Trim();
                                                                fldMatching = args[4].Trim();
                                                                break;
                                                        }

                                                        if (pEditTemp != null)
                                                        {


                                                            pNewFeat = Globals.CreateFeature(inFeature.ShapeCopy, pEditTemp, AAState._editor, ArcMap.Application, false, false, false);

                                                        }
                                                        else
                                                        {
                                                            pNewFeat = Globals.CreateFeature(inFeature.ShapeCopy, pTargetFL, AAState._editor, ArcMap.Application, false, false, false);

                                                        }
                                                        pEditTemp = null;
                                                        if (fldMatching != null)
                                                        {
                                                            if (fldMatching == "")
                                                            {
                                                                FldPairs = new string[] { };
                                                            }
                                                            else
                                                            {
                                                                FldPairs = fldMatching.Split(',');
                                                            }
                                                        }
                                                        else
                                                        {
                                                            FldPairs = new string[] { };

                                                        }

                                                        List<string> targFilds = new List<string>();

                                                        foreach (string strFlpPair in FldPairs)
                                                        {
                                                            string[] fldMatch = strFlpPair.Split('-');
                                                            if (fldMatch.Length != 2)
                                                            {
                                                                AAState.WriteLine("                  ERROR: COPY_FEATURE: Field pairing is not properly defined");
                                                            }
                                                            else
                                                            {
                                                                string strSrcFldName = fldMatch[0];
                                                                string strTarFldName = fldMatch[1];
                                                                int intSrcFldIdx = Globals.GetFieldIndex((inFeature.Class as IFeatureClass).Fields, (strSrcFldName));
                                                                int intTarFldIdx = Globals.GetFieldIndex(pTargetFL.FeatureClass.Fields, strTarFldName);
                                                                if (intSrcFldIdx == -1 || intTarFldIdx == -1)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: COPY_FEATURE: Either the source or target field was not found");
                                                                }
                                                                else
                                                                {
                                                                    targFilds.Add(strTarFldName.ToUpper());

                                                                    try
                                                                    {
                                                                        pNewFeat.set_Value(intTarFldIdx, inFeature.get_Value(intSrcFldIdx));
                                                                    }
                                                                    catch
                                                                    {
                                                                        AAState.WriteLine("                  ERROR: COPY_FEATURE: Setting value: " + strFlpPair);

                                                                    }
                                                                }

                                                            }
                                                        }
                                                        IFields pTarFields = pTargetFL.FeatureClass.Fields;
                                                        IField pTarField = null;
                                                        for (int i = 0; i < pTarFields.FieldCount; i++)
                                                        {
                                                            pTarField = pTarFields.get_Field(i);
                                                            if (pTarField.Type != esriFieldType.esriFieldTypeGlobalID &&
                                                                pTarField.Type != esriFieldType.esriFieldTypeOID &&
                                                                pTarField.Type != esriFieldType.esriFieldTypeGeometry &&
                                                                 pTarField.Name.ToUpper() != "SHAPE_LENGTH" &&
                                                                pTarField.Name.ToUpper() != "SHAPE.LEN" &&
                                                                pTarField.Name.ToUpper() != "SHAPE_AREA" &&
                                                                pTarField.Name.ToUpper() != "SHAPE.AREA")
                                                            {
                                                                if (targFilds.Contains(pTarField.Name.ToUpper()) == false)
                                                                {
                                                                    int fldIdx = inFeature.Fields.FindField(pTarField.Name);
                                                                    if (fldIdx > 0)
                                                                    {
                                                                        try
                                                                        {
                                                                            pNewFeat.set_Value(i, inFeature.get_Value(fldIdx));
                                                                        }
                                                                        catch
                                                                        {
                                                                            AAState.WriteLine("                  WARNING: COPY_FEATURE: Setting value: " + pTarField.Name);

                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }

                                                        pTarFields = null;
                                                        pTarField = null;
                                                        if (NewFeatureList == null)
                                                        {
                                                            NewFeatureList = new List<IObject>();
                                                        }


                                                        NewFeatureList.Add(pNewFeat);

                                                        if (sourceAction == "CUT")
                                                        {
                                                            MessageBox.Show("CUT is not supported at the moment");

                                                        }



                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                     ERROR: Value info was not correct");

                                                    }
                                                }

                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: COPY_FEATURE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    pTargetFL = null;

                                                    pRowCh = null;
                                                    pNewFeat = null;
                                                    AAState.WriteLine("                  Finished: COPY_FEATURE");
                                                }
                                                break;
                                            }

                                        case "VALIDATE_CONNECTIVITY":
                                            {
                                                AAState.WriteLine("                  Trying: VALIDATE_CONNECTIVITY");

                                                try
                                                {


                                                    if ((valData != null) && (inFeature != null))
                                                    {
                                                        AAState.WriteLine("                     Feature and valueinfo is valid");

                                                        AAState.WriteLine("                     Checking if feature is in a geometric network");
                                                        bool validFeat = false;
                                                        if (inFeature is INetworkFeature)
                                                        {
                                                            AAState.WriteLine("                     Feature is in a geometric network");



                                                            AAState.WriteLine("                     Splitting up value info: " + valData);

                                                            args = valData.Split('|');
                                                            int connectionCnt = Globals.getConnectionCount(inFeature);

                                                            foreach (string fldConPair in args)
                                                            {

                                                                string[] fldCon = fldConPair.Split(',');
                                                                if (fldCon.Length == 1)
                                                                {
                                                                    AAState.WriteLine("                     No values for the specified fields");
                                                                    if (Globals.IsNumeric(fldCon[0]))
                                                                    {
                                                                        if (connectionCnt == Convert.ToInt32(fldCon[0]))
                                                                        {
                                                                            AAState.WriteLine("                     Valid Connection Rule Found");
                                                                            validFeat = true;
                                                                            break;

                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                     ERROR: Connection value is not numeric");
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    if (intFldIdxs.Count == 0)
                                                                    {
                                                                        AAState.WriteLine("                     No values for the specified fields");
                                                                        if (fldCon.Length == 2)
                                                                        {
                                                                            if (connectionCnt == Convert.ToInt32(fldCon[1]))
                                                                            {
                                                                                AAState.WriteLine("                     Valid Connection Rule Found");
                                                                                validFeat = true;
                                                                                break;

                                                                            }
                                                                        }
                                                                        if (fldCon.Length > 2)
                                                                        {
                                                                            if (connectionCnt >= Convert.ToInt32(fldCon[1]) && connectionCnt <= Convert.ToInt32(fldCon[2]))
                                                                            {
                                                                                AAState.WriteLine("                     Valid Connection Rule Found");
                                                                                validFeat = true;
                                                                                break;

                                                                            }
                                                                        }

                                                                    }

                                                                    else if (inFeature.get_Value(intFldIdxs[0]).ToString() == fldCon[0])
                                                                    {


                                                                        if (fldCon.Length == 2)
                                                                        {
                                                                            if (connectionCnt == Convert.ToInt32(fldCon[1]))
                                                                            {
                                                                                AAState.WriteLine("                     Valid Connection Rule Found");
                                                                                validFeat = true;
                                                                                break;

                                                                            }
                                                                        }
                                                                        if (fldCon.Length > 2)
                                                                        {
                                                                            if (connectionCnt >= Convert.ToInt32(fldCon[1]) && connectionCnt <= Convert.ToInt32(fldCon[2]))
                                                                            {
                                                                                AAState.WriteLine("                     Valid Connection Rule Found");
                                                                                validFeat = true;
                                                                                break;

                                                                            }
                                                                        }


                                                                    }


                                                                }

                                                            }
                                                            if (validFeat == false)
                                                            {
                                                                AAState.WriteLine("                     Abort Edit");
                                                                AAState._editor.AbortOperation();
                                                                return false;

                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                     ERROR: Feature is not a geometric network feature");

                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                     ERROR: Value info was not correct");

                                                    }
                                                }

                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: VALIDATE_CONNECTIVITY" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: VALIDATE_CONNECTIVITY");
                                                }
                                                break;
                                            }

                                        case "VALIDATE_ATTRIBUTES":
                                            {
                                                AAState.WriteLine("                  Trying: VALIDATE_ATTRIBUTES");

                                                try
                                                {
                                                    if ((valData != null) && (inFeature != null))
                                                    {

                                                        AAState.WriteLine("                     Feature and valueinfo is valid");
                                                        IRowChanges pRowCh = inObject as IRowChanges;
                                                        changed = true;
                                                        if (intFldIdxs != null && intFldIdxs.Count > 0 && mode != "ON_CREATE")
                                                        {
                                                            for (int fldIdx = 0; fldIdx < intFldIdxs.Count; fldIdx++)
                                                            {
                                                                AAState.WriteLine("                     Row to monitor specified");
                                                                changed = pRowCh.get_ValueChanged(intFldIdxs[fldIdx]);
                                                                AAState.WriteLine("                     " + strFldNames[fldIdx] + " changed value was " + changed);
                                                                if (changed)
                                                                    break;
                                                            }



                                                        }
                                                        if (changed)
                                                        {
                                                            args = valData.Split('|');
                                                            args = args[0].Split(',');
                                                            AAState.WriteLine("                     Checking fields to compare: " + args);
                                                            if (args.Length > 0)
                                                            {

                                                                AAState.WriteLine("                     Getting templates for feature class");
                                                                IList<ILayer> pLayList = Globals.FindLayersByClassID(((IMxDocument)ArcMap.Application.Document).FocusMap, inObject.Class.ObjectClassID);
                                                                if (pLayList != null)
                                                                {
                                                                    if (pLayList.Count > 0)
                                                                    {


                                                                        AAState.WriteLine("                     " + pLayList.Count + " Layers found");
                                                                        bool ValidComb = false;

                                                                        foreach (ILayer pLay in pLayList)
                                                                        {
                                                                            AAState.WriteLine("                     Checking " + pLay.Name);
                                                                            if (pLay is IFeatureLayer)
                                                                            {
                                                                                AAState.WriteLine("                     Layer is a featurelayer");

                                                                                AAState.WriteLine("                     Getting Edit Template Manager");
                                                                                IEditTemplateManager pEdTmpManager = Globals.GetEditTemplateManager((IFeatureLayer)pLay);
                                                                                AAState.WriteLine("                     Checking templates");
                                                                                ValidComb = Globals.FeatureIsValidTemplate(pEdTmpManager, inFeature, args);
                                                                                AAState.WriteLine("                     Template Found Status: " + ValidComb.ToString());
                                                                                if (ValidComb == true)
                                                                                    break;

                                                                            }



                                                                        }
                                                                        if (ValidComb == false)
                                                                        {
                                                                            AAState.WriteLine("                     Abort Edit");
                                                                            AAState._editor.AbortOperation();
                                                                            return false;

                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                     WARNING: No layers where found!");

                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                     WARNING: No layers where found!");

                                                                }

                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                     Monitored fields where not changed");

                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: VALIDATE_ATTRIBUTES" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: VALIDATE_ATTRIBUTES");
                                                }
                                                break;
                                            }


                                        case "SPLIT_INTERSECTING_FEATURE":
                                            {
                                                AAState.WriteLine("                  Trying: SPLIT_INTERSECTING_FEATURE");

                                                try
                                                {
                                                    if ((valData != null) && (inFeature != null))
                                                    {
                                                        intersectLayerName = "";
                                                        intersectLayer = null;
                                                        args = valData.Split('|');
                                                        if (args.Length > 0)
                                                        {
                                                            AAState.WriteLine("                  " + args.Length + " Layers listed ");

                                                            for (int i = 0; i < args.Length; i++)
                                                            {

                                                                intersectLayerName = args[i].Trim();
                                                                AAState.WriteLine("                  Searching for " + intersectLayerName);
                                                                boolLayerOrFC = true;
                                                                if (intersectLayerName.Contains("("))
                                                                {
                                                                    string[] tempSplt = intersectLayerName.Split('(');
                                                                    intersectLayerName = tempSplt[0];
                                                                    intersectLayer = Globals.FindLayer(AAState._editor.Map, intersectLayerName, ref boolLayerOrFC) as IFeatureLayer;
                                                                    if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                    {
                                                                        boolLayerOrFC = true;
                                                                    }
                                                                    else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                    {
                                                                        boolLayerOrFC = true;
                                                                    }
                                                                    else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                    {
                                                                        boolLayerOrFC = false;
                                                                    }
                                                                    else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                    {
                                                                        boolLayerOrFC = false;
                                                                    }
                                                                    else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                    {
                                                                        boolLayerOrFC = false;
                                                                    }
                                                                    else
                                                                    {
                                                                        boolLayerOrFC = true;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    intersectLayer = Globals.FindLayer(AAState._editor.Map, intersectLayerName, ref boolLayerOrFC) as IFeatureLayer;
                                                                }
                                                                if (intersectLayer != null)
                                                                {
                                                                    AAState.WriteLine("                  Layer Found " + intersectLayerName);
                                                                    if (intersectLayer.FeatureClass != null)
                                                                    {
                                                                        AAState.WriteLine("                  Datasource is valid for " + intersectLayerName);
                                                                        double snapTol = Globals.GetXYTolerance(intersectLayer);
                                                                        sFilter = Globals.createSpatialFilter(intersectLayer, inFeature, false);

                                                                        AAState.WriteLine("                  Checking source Geometry Type");

                                                                        AAState.WriteLine("                  Searching " + intersectLayerName + "for intersected feature");

                                                                        pFS = (IFeatureSelection)intersectLayer;
                                                                        if (boolLayerOrFC)
                                                                        {
                                                                            if (pFS.SelectionSet.Count > 0)
                                                                            {
                                                                                pFS.SelectionSet.Search(sFilter, true, out cCurs);
                                                                                fCursor = cCurs as IFeatureCursor;


                                                                            }
                                                                            else
                                                                            {
                                                                                fCursor = intersectLayer.Search(sFilter, true);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            fCursor = intersectLayer.FeatureClass.Search(sFilter, true);
                                                                        }

                                                                        IFeature intsersectFeature;
                                                                        int idx = 1;
                                                                        while ((intsersectFeature = fCursor.NextFeature()) != null)
                                                                        {
                                                                            if (intsersectFeature.Class != inFeature.Class)
                                                                            {
                                                                                AAState.WriteLine("                  Splitting Intersected Feature number: " + idx);
                                                                                idx++;

                                                                                if (intsersectFeature is INetworkFeature)
                                                                                {
                                                                                    AAState.WriteLine("                  Line to split is a Geometric Network line, this operation is not valid for these types of features");
                                                                                }
                                                                                else
                                                                                {
                                                                                    ISet featset = Globals.splitLineWithPoint(intsersectFeature, inFeature.ShapeCopy as IPoint, snapTol, null, "{0:0.00}", ArcMap.Application);

                                                                                    if (featset != null)
                                                                                    {
                                                                                        AAState.WriteLine("                  Adding split features to array to call the AA ext");

                                                                                        if (featset.Count > 0)
                                                                                        {
                                                                                            if (NewFeatureList == null)
                                                                                            {
                                                                                                NewFeatureList = new List<IObject>();
                                                                                            }
                                                                                            object featobj;
                                                                                            while ((featobj = featset.Next()) != null)
                                                                                            {
                                                                                                IFeature feature = featobj as IFeature;

                                                                                                if (feature != null)
                                                                                                {
                                                                                                    NewFeatureList.Add(feature as IObject);
                                                                                                }
                                                                                                feature = null;
                                                                                            }

                                                                                        }
                                                                                        AAState.WriteLine("                  Split feature " + intersectLayerName + " into " + featset.Count);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  Split return no features");

                                                                                    }
                                                                                    featset = null;
                                                                                }
                                                                            }
                                                                            if (intsersectFeature != null)
                                                                            {
                                                                                Marshal.ReleaseComObject(intsersectFeature);
                                                                            }


                                                                            else if (intsersectFeature.Class == inFeature.Class && intsersectFeature.OID != inFeature.OID)
                                                                            {
                                                                                AAState.WriteLine("                  Splitting Intersected Feature number: " + idx);
                                                                                idx++;
                                                                                if (intsersectFeature is INetworkFeature)
                                                                                {
                                                                                    AAState.WriteLine("                  Line to split is a Geometric Network line, this operation is not valid for these types of features");
                                                                                }
                                                                                else
                                                                                {
                                                                                    ISet featset = Globals.splitLineWithPoint(intsersectFeature, inFeature.ShapeCopy as IPoint, snapTol, null, "{0:0.00}", ArcMap.Application);


                                                                                    if (featset == null)
                                                                                    {
                                                                                        AAState.WriteLine("                  Error splitting feature, the feature may be a geometric network feature");

                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  Adding split features to array to call the AA ext");

                                                                                        if (featset.Count > 0)
                                                                                        {
                                                                                            if (NewFeatureList == null)
                                                                                            {
                                                                                                NewFeatureList = new List<IObject>();
                                                                                            }
                                                                                            object featobj;
                                                                                            while ((featobj = featset.Next()) != null)
                                                                                            {
                                                                                                IFeature feature = featobj as IFeature;
                                                                                                if (feature != null)
                                                                                                {
                                                                                                    NewFeatureList.Add(feature as IObject);
                                                                                                }
                                                                                                feature = null;

                                                                                            }

                                                                                        }
                                                                                        AAState.WriteLine("                  Split feature " + intersectLayerName + " into " + featset.Count);
                                                                                    }
                                                                                    featset = null;

                                                                                }
                                                                                if (intsersectFeature != null)
                                                                                {
                                                                                    Marshal.ReleaseComObject(intsersectFeature);
                                                                                }


                                                                            }

                                                                        }
                                                                        intsersectFeature = null;
                                                                    }
                                                                }

                                                                else
                                                                {
                                                                    AAState.WriteLine("                  Warning: Can't find intersecting layer: " + intersectLayerName);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Unsupported Value Info: " + valData);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: not a feature or no Value Info");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: SPLIT_INTERSECTING_FEATURE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: SPLIT_INTERSECTING_FEATURE");
                                                }
                                                break;
                                            }

                                        case "NEAREST_FEATURE_ATTRIBUTES":
                                            {
                                                AAState.WriteLine("                  Trying NEAREST_FEATURE_ATTRIBUTES");
                                                try
                                                {
                                                    if ((valData != null) && (inFeature != null))
                                                    {
                                                        sourceLayerName = "";
                                                        string[] sourceFieldNames = null;
                                                        string[] destFieldNames = null;
                                                        searchDistance = 0;

                                                        // Parse arguments
                                                        args = valData.Split('|');
                                                        if (args.Length == 3)
                                                        {
                                                            sourceLayerName = args[0].ToString().Trim();
                                                            sourceFieldNames = args[1].ToString().Split(',');
                                                            destFieldNames = args[2].ToString().Split(',');
                                                            AAState.WriteLine("                  WARNING:  search distance as not specified, defaulting to 0");

                                                        }
                                                        else if (args.Length == 4)
                                                        {
                                                            sourceLayerName = args[0].ToString().Trim();
                                                            sourceFieldNames = args[1].ToString().Split(',');
                                                            destFieldNames = args[2].ToString().Split(',');
                                                            Double.TryParse(args[3], out searchDistance);
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR:  the valuemethod is not defined properly for this rule");
                                                            continue;

                                                        }

                                                        if ((sourceFieldNames != null) && (destFieldNames != null) &&
                                                            (sourceFieldNames.Length > 0) && (destFieldNames.Length > 0) &&
                                                            (sourceFieldNames.Length == destFieldNames.Length))
                                                        {
                                                            AAState.WriteLine("                  Looking for layer: " + sourceLayerName);

                                                            boolLayerOrFC = true;
                                                            if (sourceLayerName.Contains("("))
                                                            {
                                                                string[] tempSplt = sourceLayerName.Split('(');
                                                                sourceLayerName = tempSplt[0];
                                                                sourceLayer = Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC) as IFeatureLayer;
                                                                if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                            }

                                                            else
                                                            {
                                                                sourceLayer = Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC) as IFeatureLayer;
                                                            }
                                                            if (sourceLayer != null)
                                                            {
                                                                if (sourceLayer.FeatureClass != null)
                                                                {
                                                                    AAState.WriteLine("                  " + sourceLayer.Name + " layer Found: " + sourceLayerName);

                                                                    string missingFieldMess = null;
                                                                    int[] sourceFieldNums = new int[sourceFieldNames.Length];
                                                                    int[] destFieldNums = new int[destFieldNames.Length];
                                                                    AAState.WriteLine("                  Checking Field Mapping");

                                                                    for (int i = 0; i < sourceFieldNums.Length; i++)
                                                                    {
                                                                        int fnum = sourceLayer.FeatureClass.FindField(sourceFieldNames[i].Trim());
                                                                        if (fnum < 0)
                                                                        {
                                                                            missingFieldMess = sourceFieldNames[i].Trim() + " in table " + sourceLayerName;
                                                                            break;
                                                                        }
                                                                        sourceFieldNums[i] = fnum;
                                                                    }
                                                                    if (missingFieldMess == null)
                                                                    {
                                                                        for (int i = 0; i < destFieldNums.Length; i++)
                                                                        {
                                                                            int fnum = inFeature.Fields.FindField(destFieldNames[i].Trim());
                                                                            if (fnum < 0)
                                                                            {
                                                                                missingFieldMess = destFieldNames[i].Trim() + " in table " + tableName;
                                                                                break;
                                                                            }
                                                                            destFieldNums[i] = fnum;
                                                                        }
                                                                    }
                                                                    if (missingFieldMess == null)
                                                                    {
                                                                        AAState.WriteLine("                  Field Mapping verified");

                                                                        // found source and destination fields.

                                                                        if (searchDistance > 0)
                                                                        {
                                                                            sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, searchDistance, false);


                                                                        }
                                                                        else
                                                                        {
                                                                            sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, searchDistance, false);

                                                                        }


                                                                        AAState.WriteLine("                  Searching for Nearest Feature");

                                                                        pFS = (IFeatureSelection)sourceLayer;
                                                                        if (boolLayerOrFC)
                                                                        {
                                                                            if (pFS.SelectionSet.Count > 0)
                                                                            {
                                                                                pFS.SelectionSet.Search(sFilter, false, out cCurs);
                                                                                fCursor = cCurs as IFeatureCursor;


                                                                            }
                                                                            else
                                                                            {
                                                                                fCursor = sourceLayer.Search(sFilter, false);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            fCursor = sourceLayer.FeatureClass.Search(sFilter, false);
                                                                        }

                                                                        sourceFeature = fCursor.NextFeature();
                                                                        nearestFeature = null;

                                                                        proxOp = (IProximityOperator)inFeature.Shape;
                                                                        lastDistance = searchDistance;
                                                                        if (sourceFeature != null)
                                                                        {
                                                                            AAState.WriteLine("                  Features Found, looping for closest");

                                                                            while (sourceFeature != null)
                                                                            {
                                                                                if (sourceFeature.Class != inFeature.Class)
                                                                                {


                                                                                    IGeometry pTempGeo = sourceFeature.ShapeCopy;
                                                                                    pTempGeo.Project(inFeature.Shape.SpatialReference);

                                                                                    distance = proxOp.ReturnDistance(pTempGeo);
                                                                                    pTempGeo = null;
                                                                                    if (distance <= lastDistance)
                                                                                    {
                                                                                        nearestFeature = sourceFeature;
                                                                                        lastDistance = distance;
                                                                                    }
                                                                                }
                                                                                else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                                                {

                                                                                    IGeometry pTempGeo = sourceFeature.ShapeCopy;
                                                                                    pTempGeo.Project(inFeature.Shape.SpatialReference);

                                                                                    distance = proxOp.ReturnDistance(pTempGeo);
                                                                                    pTempGeo = null;
                                                                                    if (distance <= lastDistance)
                                                                                    {
                                                                                        nearestFeature = sourceFeature;
                                                                                        lastDistance = distance;
                                                                                    }
                                                                                }
                                                                                sourceFeature = fCursor.NextFeature();
                                                                            }
                                                                        }
                                                                        if (nearestFeature != null)
                                                                        {
                                                                            AAState.WriteLine("                  Closest Feature is " + lastDistance + " Away with OID of " + nearestFeature.OID);


                                                                            for (int i = 0; i < sourceFieldNums.Length; i++)
                                                                            {
                                                                                try
                                                                                {
                                                                                    AAState.WriteLine("                  Trying to copy " + sourceFieldNames[i] + " to " + destFieldNames[i]);

                                                                                    inObject.set_Value(destFieldNums[i], nearestFeature.get_Value(sourceFieldNums[i]));
                                                                                }
                                                                                catch
                                                                                {
                                                                                    AAState.WriteLine("                  ERROR: copying " + sourceFieldNames[i] + " to " + destFieldNames[i]);

                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  No Feature was found, default fields");

                                                                            for (int i = 0; i < destFieldNums.Length; i++)
                                                                            {
                                                                                IField field = inObject.Fields.get_Field(destFieldNums[i]);
                                                                                object newval = field.DefaultValue;
                                                                                if (newval == null)
                                                                                {
                                                                                    if (field.IsNullable)
                                                                                    {
                                                                                        inObject.set_Value(destFieldNums[i], null);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    inObject.set_Value(destFieldNums[i], newval);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR: Cant find field " + missingFieldMess);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayerName + " data source is not set");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: " + sourceLayerName + " was not found");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Invalid Value Info: " + valData);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Not a feature or missing Value Info");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: NEAREST_FEATURE_ATTRIBUTES" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: NEAREST_FEATURE_ATTRIBUTES");
                                                }
                                                break;
                                            }
                                        case "MINIMUM_LENGTH":
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying MINIMUM_LENGTH");

                                                    double minlength;
                                                    AAState.WriteLine("                  Evaluating Minimum length value");

                                                    if (Double.TryParse(valData, out minlength))
                                                    {
                                                        if (inFeature != null)
                                                        {
                                                            ICurve curve = inFeature.Shape as ICurve;
                                                            if (curve != null)
                                                            {
                                                                if (curve.Length < minlength)
                                                                {
                                                                    String mess = "Line is shorter than " +
                                                                        String.Format("{0:0.00}", minlength) + " " + Globals.GetSpatRefUnitName(inFeature.Shape.SpatialReference, true) +
                                                                        ", aborting edit.";
                                                                    AAState.WriteLine("                  " + mess);

                                                                    MessageBox.Show(mess, "Line too short");
                                                                    AAState._editor.AbortOperation();
                                                                    return false;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR:  Feature is not a Line");

                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  Error MINIMUM_LENGTH \n" + ex.Message);
                                                }
                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished MINIMUM_LENGTH");

                                                }

                                                break;
                                            }
                                        case "LINK_TABLE_ASSET":

                                            try
                                            {
                                                intersectLayerName = "";
                                                intersectTable = null;
                                                intersectLayer = null;
                                                List<string> intersectLayerFieldNameList = new List<string>();
                                                List<int> intersectFieldPosList = new List<int>();
                                                AAState.WriteLine("                  Trying LINK_TABLE_ASSET");
                                                args = valData.Split('|');
                                                switch (args.GetLength(0))
                                                {
                                                    case 1:  // Feature Layer only
                                                        intersectLayerName = args[0].ToString();
                                                        break;
                                                    case 2:  // Feature Layer| Field to copy
                                                        intersectLayerName = args[0].ToString();


                                                        intersectLayerFieldNameList = new List<string>(args[1].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

                                                        break;
                                                    case 3:  // Feature Layer| Field to copy | for future
                                                        intersectLayerName = args[0].ToString();
                                                        intersectLayerFieldNameList = new List<string>(args[1].ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

                                                        break;
                                                    default:
                                                        AAState.WriteLine("                  ERROR: Unsupported Value Method: " + valData);
                                                        continue;

                                                }
                                                bool FCorLayerIntersect = true;
                                                intersectLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, intersectLayerName, ref FCorLayerIntersect);
                                                intersectTable = Globals.FindStandAloneTable(AAState._editor.Map, intersectLayerName);

                                                if (intersectLayer != null)
                                                {
                                                    //Find Area Field

                                                    foreach (string intersectLayerFieldName in intersectLayerFieldNameList)
                                                    {

                                                        intersectFieldPos = intersectLayer.FeatureClass.Fields.FindField(intersectLayerFieldName);
                                                        if (intersectFieldPos < 0)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Asset feature Layer Field(" + intersectLayerFieldName + ") not found");
                                                            break;
                                                        }

                                                        else
                                                        {
                                                            intersectFieldPosList.Add(intersectFieldPos);
                                                        }
                                                    }
                                                    intersectLayerSelection = (IFeatureSelection)intersectLayer;
                                                    if (intersectLayerSelection.SelectionSet.Count == 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: No assests selected in " + intersectLayerName);

                                                        break;
                                                    }
                                                    if (intersectLayerSelection.SelectionSet.Count > 1)
                                                    {
                                                        AAState.WriteLine("                  ERROR: To many assests are selected in " + intersectLayerName);

                                                        break;
                                                    }

                                                    intersectLayerSelection.SelectionSet.Search(null, true, out cCurs);
                                                }
                                                else if (intersectTable != null)
                                                {
                                                    if (intersectTable.Table == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Asset  Layer(" + intersectLayerName + ") not found");
                                                        break;
                                                    }

                                                    foreach (string intersectLayerFieldName in intersectLayerFieldNameList)
                                                    {

                                                        intersectFieldPos = intersectTable.Table.Fields.FindField(intersectLayerFieldName);
                                                        if (intersectFieldPos < 0)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Asset  Layer Field(" + intersectLayerFieldName + ") not found");
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            intersectFieldPosList.Add(intersectFieldPos);
                                                        }
                                                    }

                                                    intersectTableSelection = (ITableSelection)intersectTable;
                                                    if (intersectTableSelection.SelectionSet.Count == 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: No assests selected in " + intersectLayerName);

                                                        break;
                                                    }
                                                    if (intersectTableSelection.SelectionSet.Count > 1)
                                                    {
                                                        AAState.WriteLine("                  ERROR: To many assests are selected in " + intersectLayerName);

                                                        break;
                                                    }

                                                    intersectTableSelection.SelectionSet.Search(null, true, out cCurs);
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: Asset  Layer(" + intersectLayerName + ") not found");
                                                    break;
                                                }




                                                IRow row;


                                                while ((row = cCurs.NextRow()) != null)
                                                {
                                                    int idx = 0;
                                                    foreach (int fldIdxInt in intersectFieldPosList)
                                                    {
                                                        if (idx >= intFldIdxs.Count)
                                                            continue;

                                                        string val = row.get_Value(fldIdxInt).ToString();
                                                        if (inObject.Fields.get_Field(intFldIdxs[idx]).Type == esriFieldType.esriFieldTypeString)
                                                            inObject.set_Value(intFldIdxs[idx], val);
                                                        else if (inObject.Fields.get_Field(intFldIdxs[idx]).Type == esriFieldType.esriFieldTypeSmallInteger || inObject.Fields.get_Field(intFldIdxs[idx]).Type == esriFieldType.esriFieldTypeInteger)
                                                        {
                                                            if (Globals.IsNumeric(val))
                                                            {
                                                                inObject.set_Value(intFldIdxs[idx], Convert.ToInt32(val));

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: Field is a number and Value is not" + val);

                                                            }
                                                        }
                                                        else if (inObject.Fields.get_Field(intFldIdxs[idx]).Type == esriFieldType.esriFieldTypeSingle || inObject.Fields.get_Field(intFldIdxs[idx]).Type == esriFieldType.esriFieldTypeDouble)
                                                        {
                                                            if (Globals.IsNumeric(val))
                                                            {
                                                                inObject.set_Value(intFldIdxs[idx], Convert.ToDouble(val));

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: Field is a number and Value is not:" + val);

                                                            }
                                                        }
                                                        else
                                                        {
                                                            inObject.set_Value(intFldIdxs[idx], val);
                                                        }
                                                        idx++;

                                                    }



                                                }
                                                if (row != null)
                                                    Marshal.ReleaseComObject(cCurs);

                                                row = null;

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: LINK_TABLE_ASSET" + Environment.NewLine + ex.Message);
                                            }

                                            finally
                                            {
                                                if (cCurs != null)
                                                {
                                                    Marshal.ReleaseComObject(cCurs);
                                                    GC.Collect(300);
                                                    GC.WaitForFullGCComplete();
                                                    cCurs = null;

                                                }
                                                AAState.WriteLine("                  Finished: LINK_TABLE_ASSET");
                                            }
                                            break;

                                        case "GET_ADDRESS_FROM_CENTERLINE":

                                            AAState.WriteLine("                  Trying GET_ADDRESS_FROM_CENTERLINE");
                                            List<IPoint> pPnts = null;
                                            try
                                            {
                                                if ((valData != null) && (inFeature != null))
                                                {
                                                    sourceLayerName = "";
                                                    string[] sourceFieldNames = null;

                                                    searchDistance = 0;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.Length == 2)
                                                    {
                                                        sourceLayerName = args[0].ToString().Trim();
                                                        sourceFieldNames = args[1].ToString().Split(',');
                                                        searchDistance = 2;
                                                        AAState.WriteLine("                  WARNING:  search distance as not specified, defaulting to 0");

                                                    }
                                                    else if (args.Length == 3)
                                                    {
                                                        sourceLayerName = args[0].ToString().Trim();
                                                        sourceFieldNames = args[1].ToString().Split(',');
                                                        Double.TryParse(args[2], out searchDistance);

                                                    }

                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR:  the valuemethod is not defined properly for this rule");
                                                        continue;

                                                    }

                                                    if (sourceFieldNames.Length != 5)
                                                    {
                                                        AAState.WriteLine("                  ERROR:  the valuemethod fields part does not have enough fields listed");
                                                        continue;

                                                    }


                                                    boolLayerOrFC = false;
                                                    if (sourceLayerName.Contains("("))
                                                    {
                                                        string[] tempSplt = sourceLayerName.Split('(');
                                                        sourceLayerName = tempSplt[0];
                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                        {
                                                            boolLayerOrFC = true;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                        {
                                                            boolLayerOrFC = true;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                        {
                                                            boolLayerOrFC = false;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                        {
                                                            boolLayerOrFC = false;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                        {
                                                            boolLayerOrFC = false;
                                                        }
                                                        else
                                                        {
                                                            boolLayerOrFC = true;
                                                        }
                                                    }
                                                    pPnts = Globals.GetGeomCenter(inObject as IFeature);
                                                    if (pPnts.Count != 0)
                                                    {



                                                        AddressInfo pRetValu = Globals.GetAddressInfo(ArcMap.Application, pPnts[0] as IPoint, sourceLayerName,
                                                            sourceFieldNames[0].Trim(), sourceFieldNames[1].Trim(), sourceFieldNames[2].Trim(), sourceFieldNames[3].Trim(), sourceFieldNames[4].Trim(), false, searchDistance);
                                                        if (pRetValu != null)
                                                        {
                                                            if (pRetValu.Messages == "")
                                                            {

                                                                bool rightSide = true;
                                                                IPoint pPnt = Globals.GetPointOnLine((inObject as IFeature).Shape as IPoint, pRetValu.StreetGeometry as IPolyline, 400, out rightSide);

                                                                try
                                                                {
                                                                    if (strFldNames.Count == 2)
                                                                    {
                                                                        if (intFldIdxs[0] != -1)
                                                                        {
                                                                            if (rightSide)
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], pRetValu.RightAddress);
                                                                            }
                                                                            else
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], pRetValu.LeftAddress);
                                                                            }
                                                                        }
                                                                        if (intFldIdxs[1] != -1)
                                                                            inObject.set_Value(intFldIdxs[1], pRetValu.StreetName);


                                                                    }
                                                                    else
                                                                    {
                                                                        if (intFldIdxs[0] != -1)
                                                                        {
                                                                            if (rightSide)
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], pRetValu.RightAddress + " " + pRetValu.StreetName);
                                                                            }
                                                                            else
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], pRetValu.LeftAddress + " " + pRetValu.StreetName);
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: Error setting field values, GET_ADDRESS_FROM_CENTERLINE" + Environment.NewLine + ex.Message);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: Error getting address info from the centerline: " + pRetValu.Messages);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Error getting address info from the centerline, GET_ADDRESS_FROM_CENTERLINE");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Error getting Location, GET_ADDRESS_FROM_CENTERLINE");
                                                    }

                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: GET_ADDRESS_FROM_CENTERLINE" + Environment.NewLine + ex.Message);
                                            }

                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: GET_ADDRESS_FROM_CENTERLINE");
                                                pPnts = null;

                                            }

                                            break;


                                        case "GET_ADDRESS_USING_GEOCODER":
                                            {
                                                IReverseGeocoding reverseGeocoding = null;
                                                IAddressGeocoding addressGeocoding = null;
                                                IPoint revGCLoc = null;
                                                IFields matchFields = null;
                                                IField shapeField = null;

                                                IReverseGeocodingProperties reverseGeocodingProperties = null;
                                                IPropertySet addressProperties = null;


                                                IAddressInputs addressInputs = null;
                                                IFields addressFields = null;
                                                object key = null;
                                                object value = null;
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying GET_ADDRESS_USING_GEOCODER");

                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 2)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    AAState.WriteLine("                  Trying to get address locator");
                                                    reverseGeocoding = Globals.OpenLocator(args[0], args[1]);


                                                    if (reverseGeocoding == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Reverse Address not locator found");
                                                        break;
                                                    }
                                                    AAState.WriteLine("                  Reverse Address locator found");
                                                    AAState.WriteLine("                  Retrieving Location to reverse geocode");
                                                    revGCLoc = Globals.GetGeomCenter(inFeature)[0];
                                                    AAState.WriteLine("                  Location retrieved");
                                                    // Create a Point at which to find the address.
                                                    addressGeocoding = (IAddressGeocoding)reverseGeocoding;

                                                    matchFields = addressGeocoding.MatchFields;
                                                    int shpFld = matchFields.FindField("Shape");

                                                    shapeField = matchFields.get_Field(shpFld);
                                                    AAState.WriteLine("                  Setting distance");
                                                    // Set the search tolerance for reverse geocoding.
                                                    reverseGeocodingProperties = (IReverseGeocodingProperties)reverseGeocoding;
                                                    reverseGeocodingProperties.SearchDistance = 100;
                                                    reverseGeocodingProperties.SearchDistanceUnits = esriUnits.esriFeet;
                                                    reverseGeocoding.InitDefaults();

                                                    // Find the address nearest the Point.
                                                    addressProperties = reverseGeocoding.ReverseGeocode(revGCLoc, false);

                                                    // Print the address properties.
                                                    addressInputs = (IAddressInputs)reverseGeocoding;
                                                    addressFields = addressInputs.AddressFields;

                                                    addressProperties.GetAllProperties(out key, out value);


                                                    string tempVal = "";
                                                    for (int i = 0; i < addressFields.FieldCount; i++)
                                                    {
                                                        IField addressField = addressFields.get_Field(i);
                                                        if (tempVal == "")
                                                        {
                                                            tempVal = addressProperties.GetProperty(addressField.Name).ToString();
                                                        }
                                                        else
                                                        {
                                                            tempVal = tempVal + ", " + addressProperties.GetProperty(addressField.Name).ToString();
                                                        }

                                                    }
                                                    inFeature.set_Value(intFldIdxs[0], tempVal);

                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: GET_ADDRESS_USING_GEOCODER" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: GET_ADDRESS_USING_GEOCODER");
                                                    reverseGeocoding = null;
                                                    addressGeocoding = null;
                                                    revGCLoc = null;
                                                    matchFields = null;
                                                    shapeField = null;

                                                    reverseGeocodingProperties = null;
                                                    addressProperties = null;


                                                    addressInputs = null;
                                                    addressFields = null;
                                                    key = null;
                                                    value = null;
                                                }
                                                break;

                                            }



                                        case "GET_ADDRESS_USING_ARCGIS_SERVICE":  //ARGS: url

                                            try
                                            {
                                                IPoint revGCLoc = null;
                                                AAState.WriteLine("                  Trying GET_ADDRESS_USING_ARCGIS_SERVICE");
                                                if (fieldObj.Type == esriFieldType.esriFieldTypeString)
                                                {
                                                    if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                    {
                                                        args = valData.Split('|');
                                                        revGCLoc = Globals.GetGeomCenter(inFeature)[0];
                                                        int wkid = inFeature.Shape.SpatialReference.FactoryCode;
                                                        if (revGCLoc != null)
                                                        {
                                                            //Test for user specified URL 
                                                            if (valData.Trim() != "")
                                                            {
                                                                if (args.Length == 2)
                                                                {
                                                                    wkid = Convert.ToInt32(args[1]);
                                                                }
                                                                if (Globals.IsUrl(args[0]))
                                                                {
                                                                    locatorURL = args[0];

                                                                    if (!(locatorURL.EndsWith(reverseGeocodeStr)))
                                                                    {
                                                                        if (!(locatorURL.EndsWith(GeocodeStr)))
                                                                        {
                                                                            if (!(locatorURL.EndsWith("/")))
                                                                            {
                                                                                locatorURL += "/" + GeocodeStr + "/" + reverseGeocodeStr;
                                                                            }
                                                                            else
                                                                            {
                                                                                locatorURL += GeocodeStr + "/" + reverseGeocodeStr;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            if (!(locatorURL.EndsWith("/")))
                                                                            {
                                                                                locatorURL += "/" + reverseGeocodeStr;
                                                                            }
                                                                            else
                                                                            {
                                                                                locatorURL += reverseGeocodeStr;
                                                                            }
                                                                        }
                                                                    }
                                                                }



                                                                else if (args[0] == "TA_Streets_US_10")
                                                                {
                                                                    locatorURL = _agsOnlineLocators + args[0] + GeocodeStr + "/" + reverseGeocodeStr;
                                                                    //       wkid = 102100;
                                                                }
                                                                else if (args[0] == "TA_Address_NA_10" || args[0] == "TA_Address_EU")
                                                                {
                                                                    locatorURL = _agsOnlineLocators + args[0] + GeocodeStr + "/" + reverseGeocodeStr;
                                                                    //    wkid = 4326;
                                                                }
                                                                //Default to AGS Online USA geocode service
                                                                else if (_agsOnlineLocators.Substring(_agsOnlineLocators.LastIndexOf('/', _agsOnlineLocators.Length - 2)).Contains("Locator"))
                                                                {
                                                                    locatorURL = _agsOnlineLocators + "TA_Address_NA_10" + GeocodeStr + "/" + reverseGeocodeStr;
                                                                    //        wkid = 4326;
                                                                }
                                                                else
                                                                {
                                                                    locatorURL = _agsOnlineLocators + GeocodeStr + "/" + reverseGeocodeStr; ;
                                                                    //     wkid = 4326;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (_agsOnlineLocators.Substring(_agsOnlineLocators.LastIndexOf('/', _agsOnlineLocators.Length - 2)).Contains(GeocodeStr))
                                                                {
                                                                    locatorURL = _agsOnlineLocators + "/" + reverseGeocodeStr;
                                                                    //      wkid = 4326;
                                                                }
                                                                else
                                                                {
                                                                    _agsOnlineLocators = _agsOnlineLocators + "/" + GeocodeStr;
                                                                    locatorURL = _agsOnlineLocators + "/" + reverseGeocodeStr;
                                                                    // wkid = 4326;
                                                                }
                                                            }

                                                            if (!locatorURL.ToUpper().Contains("/REST/"))
                                                            {
                                                                AAState.WriteLine("                  ERROR: Thre url to the geocoder is not to the rest endpoint");
                                                            }
                                                            else
                                                            {
                                                                //Copy point from this current feature 
                                                                _copyPoint = revGCLoc;//inFeature.ShapeCopy as IPoint;



                                                                StreamReader reader = null;
                                                                HttpWebRequest request = null;
                                                                try
                                                                {
                                                                    // Create the web request  
                                                                    request = WebRequest.Create(_agsOnlineLocators) as HttpWebRequest;

                                                                    // Get response  

                                                                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                                                                    {
                                                                        // Get the response stream  
                                                                        reader = new StreamReader(response.GetResponseStream());
                                                                        string resp = reader.ReadToEnd();
                                                                        resp = resp.Substring(resp.IndexOf("Spatial Reference:"));
                                                                        resp = resp.Substring(0, resp.IndexOf("<br/>"));
                                                                        resp = resp.Substring(resp.IndexOf("</b>") + 4);
                                                                        wkid = Convert.ToInt32(resp.Split(' ')[0]);

                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                    AAState.WriteLine("                  Error getting service projection information");
                                                                    wkid = 4326;
                                                                }
                                                                finally
                                                                {


                                                                    reader = null;
                                                                    request = null;
                                                                }



                                                                _copyPoint.Project(Globals.CreateSpatRef(wkid));
                                                                //Include location parameters in URL
                                                                string results = Globals.FormatLocationRequest(locatorURL, _copyPoint.X, _copyPoint.Y, 100);
                                                                //Send and receieve the request
                                                                WebRequest req = null;
                                                                WebResponse res = null;

                                                                XmlDictionaryReader xr = null;
                                                                XmlDocument doc = null;
                                                                try
                                                                {
                                                                    req = WebRequest.Create(results);
                                                                    res = req.GetResponse();


                                                                    //Convert response from JSON to XML
                                                                    doc = new XmlDocument();
                                                                    using (Stream s = res.GetResponseStream())
                                                                    {
                                                                        xr = JsonReaderWriterFactory.CreateJsonReader(s, XmlDictionaryReaderQuotas.Max);
                                                                        doc.Load(xr);
                                                                        xr.Close();
                                                                        s.Close();
                                                                        string val = "";

                                                                        for (int h = 0; h < doc.DocumentElement.FirstChild.ChildNodes.Count - 1; h++)
                                                                        {
                                                                            if (doc.DocumentElement.FirstChild.ChildNodes[h].Name.Contains("Match") == false)
                                                                            {
                                                                                if (val == "")
                                                                                {
                                                                                    val = doc.DocumentElement.FirstChild.ChildNodes[h].InnerText;
                                                                                }
                                                                                else
                                                                                {
                                                                                    val = val + ", " + doc.DocumentElement.FirstChild.ChildNodes[h].InnerText;
                                                                                }
                                                                            }

                                                                        }
                                                                        val = val.Trim();
                                                                        inFeature.set_Value(intFldIdxs[0], val);

                                                                    }
                                                                }
                                                                catch
                                                                {
                                                                }
                                                                finally
                                                                {
                                                                    req = null;
                                                                    res = null;

                                                                    xr = null;
                                                                    doc = null;
                                                                }

                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Could not get location to Reverse Geocode");
                                                        }
                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: GET_ADDRESS_USING_ARCGIS_SERVICE: " + ex.Message);
                                            }

                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: GET_ADDRESS_USING_ARCGIS_SERVICE");
                                            }
                                            break;
                                        case "TIMESTAMP":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: TIMESTAMP");

                                                if (!String.IsNullOrEmpty(valData))
                                                {
                                                    args = valData.Split('|');
                                                    if (args.Length > 0)
                                                    {

                                                        try
                                                        {

                                                            if (fieldObj.Type == esriFieldType.esriFieldTypeDate)
                                                            {
                                                                if (args[0].ToString().ToUpper() == "DATE")
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.Date);
                                                                }
                                                                else if (args[0].ToString().Trim() != "")
                                                                {
                                                                    try
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString(args[0]));
                                                                    }
                                                                    catch
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString());
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now);
                                                                }
                                                            }
                                                            else if (fieldObj.Type == esriFieldType.esriFieldTypeString)
                                                            {
                                                                if (args[0].ToString().ToUpper() == "DATE")
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.Date);
                                                                }
                                                                else if (args[0].ToString().ToUpper() == "TIME")
                                                                {

                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString("hh:mm:ss tt"));
                                                                }
                                                                else if (args[0].ToString().ToUpper() == "TIME24")
                                                                {
                                                                    //  ReadOnlyCollection<System.TimeZoneInfo> timeZones = System.TimeZoneInfo.GetSystemTimeZones();
                                                                    //  string s = System.TimeZoneInfo.ConvertTime(DateTime.Now, timeZones[0]).ToString("HH:mm:ss");


                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString("HH:mm:ss"));
                                                                }
                                                                else if (args[0].ToString().ToUpper() == "YEAR")
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.Year.ToString());
                                                                }
                                                                else if (args[0].ToString().ToUpper() == "MONTH")
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.Month.ToString());
                                                                }
                                                                else if (args[0].ToString().ToUpper() == "DAY")
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.DayOfWeek.ToString());
                                                                }
                                                                else if (args[0].ToString().Trim() != "")
                                                                {
                                                                    try
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString(args[0]));
                                                                    }
                                                                    catch
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString());
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString());
                                                                }
                                                            }

                                                        }
                                                        catch
                                                        {
                                                            AAState.WriteLine("                  ERROR: Date/Time Format is invalid");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Value info is empty");
                                                        if (fieldObj.Type == esriFieldType.esriFieldTypeDate)
                                                            inObject.set_Value(intFldIdxs[0], DateTime.Now);
                                                        else if (fieldObj.Type == esriFieldType.esriFieldTypeString)
                                                            inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString());

                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  Value info is empty");
                                                    if (fieldObj.Type == esriFieldType.esriFieldTypeDate)
                                                        inObject.set_Value(intFldIdxs[0], DateTime.Now);
                                                    else if (fieldObj.Type == esriFieldType.esriFieldTypeString)
                                                        inObject.set_Value(intFldIdxs[0], DateTime.Now.ToString());

                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TIMESTAMP " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TIMESTAMP");
                                            }
                                            break;

                                        case "LAST_VALUE":
                                            try
                                            {

                                                AAState.WriteLine("                  Trying: LAST_VALUE");
                                                bool CheckForValue = false;
                                                if (!String.IsNullOrEmpty(valData))
                                                {
                                                    args = valData.Split('|');
                                                    if (args.Length > 0)
                                                    {
                                                        if (args[0].ToString().ToUpper() == "TRUE")
                                                        {
                                                            CheckForValue = true;
                                                        }
                                                    }

                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  Value info is empty");
                                                }
                                                if (CheckForValue && (inObject.get_Value(intFldIdxs[0]) != null && inObject.get_Value(intFldIdxs[0]) != DBNull.Value))
                                                {

                                                }
                                                else
                                                {
                                                    if (mode == "ON_CREATE")
                                                    {

                                                        lastValue = AAState.lastValueProperties.GetProperty(strFldNames[0]) as LastValueEntry;
                                                        if (lastValue == null)
                                                        {
                                                            if (inObject.get_Value(intFldIdxs[0]) != null)
                                                            {
                                                                if (inObject.get_Value(intFldIdxs[0]) != DBNull.Value)
                                                                {
                                                                    AAState.lastValueProperties.SetProperty(strFldNames[0], inObject.get_Value(intFldIdxs[0]));
                                                                }
                                                            }
                                                        }
                                                        else if (lastValue.Value != null)
                                                        {
                                                            if (lastValue.Value != DBNull.Value)
                                                            {
                                                                inObject.set_Value(intFldIdxs[0], lastValue.Value);

                                                                AAState.WriteLine("                  " + strFldNames[0] + ": " + lastValue.Value);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (inObject.get_Value(intFldIdxs[0]) != null)
                                                            {
                                                                if (inObject.get_Value(intFldIdxs[0]) != DBNull.Value)
                                                                {
                                                                    AAState.lastValueProperties.SetProperty(strFldNames[0], inObject.get_Value(intFldIdxs[0]));
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (mode == "ON_CHANGE")
                                                    {
                                                        IRowChanges pRowCh = inObject as IRowChanges;
                                                        changed = pRowCh.get_ValueChanged(intFldIdxs[0]);
                                                        if (!changed)
                                                        {
                                                            lastValue = AAState.lastValueProperties.GetProperty(strFldNames[0]) as LastValueEntry;
                                                            if (lastValue != null)
                                                            {
                                                                if (lastValue.Value != null)
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], lastValue.Value);
                                                                    AAState.WriteLine("                  " + strFldNames[0] + ": " + lastValue.Value);
                                                                }

                                                            }
                                                        }
                                                    }

                                                }
                                            }
                                            catch (Exception ex)
                                            {

                                                AAState.WriteLine("                  ERROR: LAST_VALUE " + ex.Message);


                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LAST_VALUE");
                                            }
                                            break;

                                        case "X_COORDINATE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: X_COORDINATE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {
                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.Shape as IPoint;
                                                        inFeature.set_Value(intFldIdxs[0], _copyPoint.X);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.Shape as IPolyline;
                                                        if (valData.Trim() == "")
                                                        {
                                                            inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].X);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.FromPoint.X);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.ToPoint.X);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].X);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        _copyPolygon = inFeature.Shape as IPolygon;
                                                        inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolygon)[0].X);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }


                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: X_COORDINATE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: X_COORDINATE");
                                            }
                                            break;

                                        case "Y_COORDINATE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: Y_COORDINATE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {
                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.Shape as IPoint;
                                                        inFeature.set_Value(intFldIdxs[0], _copyPoint.Y);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.Shape as IPolyline;
                                                        if (valData == "")
                                                        {
                                                            inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].Y);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.FromPoint.Y);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.ToPoint.Y);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].Y);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        _copyPolygon = inFeature.ShapeCopy as IPolygon;

                                                        inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolygon)[0].Y);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: Y_COORDINATE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: Y_COORDINATE");
                                            }
                                            break;

                                        case "LATITUDE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: LATITUDE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {

                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.ShapeCopy as IPoint;
                                                        _copyPoint.Project(AAState._sr1);

                                                        inFeature.set_Value(intFldIdxs[0], _copyPoint.Y);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.ShapeCopy as IPolyline;
                                                        _copyPolyline.Project(AAState._sr1);

                                                        if (valData == "")
                                                        {
                                                            inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].Y);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.FromPoint.Y);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.ToPoint.Y);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].Y);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        _copyPolygon = inFeature.ShapeCopy as IPolygon;
                                                        _copyPolygon.Project(AAState._sr1);

                                                        inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolygon)[0].Y);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  Error: LATITUDE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LATITUDE");
                                            }
                                            break;

                                        case "LONGITUDE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: LONGITUDE");
                                                if ((inFeature != null) && (inFeature.Shape != null) && !(inFeature.Shape.IsEmpty))
                                                {

                                                    if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPoint)
                                                    {
                                                        _copyPoint = inFeature.ShapeCopy as IPoint;
                                                        _copyPoint.Project(AAState._sr1);

                                                        inFeature.set_Value(intFldIdxs[0], _copyPoint.X);
                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                    {
                                                        _copyPolyline = inFeature.Shape as IPolyline;
                                                        _copyPolyline.Project(AAState._sr1);

                                                        if (valData == "")
                                                        {
                                                            inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].X);
                                                        }
                                                        else
                                                        {
                                                            args = valData.Split('|');
                                                            if (args[0].ToUpper() == "S")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.FromPoint.X);
                                                            }
                                                            else if (args[0].ToUpper() == "E")
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], _copyPolyline.ToPoint.X);
                                                            }
                                                            else
                                                            {
                                                                inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolyline)[0].X);
                                                            }

                                                        }


                                                    }
                                                    else if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolygon)
                                                    {
                                                        _copyPolygon = inFeature.ShapeCopy as IPolygon;
                                                        _copyPolygon.Project(AAState._sr1);

                                                        inFeature.set_Value(intFldIdxs[0], Globals.GetGeomCenter(_copyPolygon)[0].X);
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Unsupported Geometry");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: LONGITUDE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LONGITUDE");
                                            }
                                            break;

                                        case "FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: Field");
                                                // verify that field to copy exists

                                                if (!String.IsNullOrEmpty(valData))
                                                {
                                                    args = valData.Split('|');
                                                    fieldCopy = inObject.Fields.FindField(args[0] as string);

                                                    if (fieldCopy > -1)
                                                    {
                                                        bool useDisplayValue = true;
                                                        if (args.Length == 2)
                                                        {
                                                            if (args[1].ToUpper() == "CODE")
                                                                useDisplayValue = false;
                                                        }

                                                        try
                                                        {
                                                            if (useDisplayValue)
                                                            {

                                                                inObject.set_Value(intFldIdxs[0], Globals.GetDomainDisplay(inObject.get_Value(fieldCopy), inObject as IFeature, inObject.Fields.get_Field(fieldCopy)));
                                                                AAState.WriteLine("                  Value set");
                                                            }

                                                            else
                                                            {
                                                                inObject.set_Value(intFldIdxs[0], inObject.get_Value(fieldCopy));
                                                                AAState.WriteLine("                  Value set");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            AAState.WriteLine("                  ERROR: " + ex.Message);
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  " + valData + " is not found");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: Field: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: Field");
                                            }
                                            break;

                                        //CURRENT_USER
                                        //Value Data options:
                                        //U - windows username only 
                                        //W or (blank) - full username including domain i.e. domain\username
                                        //D - database user if available and not dbo
                                        case "CURRENT_USER":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: CURRENT_USER");

                                                lastEditorName = AAState._currentUserInfo.GetCurrentUser(valData, fieldObj.Length);
                                                AAState.WriteLine("                  Usernmae: " + lastEditorName);

                                                if (!String.IsNullOrEmpty(lastEditorName))
                                                {
                                                    inObject.set_Value(intFldIdxs[0], lastEditorName);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: CURRENT_USER: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: CURRENT_USER");
                                            }
                                            break;

                                        case "JUNCTION_ROTATION":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: JUNCTION_ROTATION");
                                                if ((inFeature != null))
                                                {
                                                    AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolGeographic;
                                                    args = null;
                                                    AAState.rCalc.UseDiameter = false;
                                                    AAState.rCalc.DiameterFieldName = "";
                                                    AAState.rCalc.SpinAngle = 0;

                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length == 0)
                                                        {

                                                            AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolGeographic;
                                                        }
                                                        else if (args.Length == 1)
                                                        {
                                                            if (args[0].Substring(0, 1).ToLower() == "a")
                                                                AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolArithmetic;


                                                        }
                                                        else if (args.Length == 2)
                                                        {
                                                            if (args[0].Substring(0, 1).ToLower() == "a")
                                                                AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolArithmetic;
                                                            if (Globals.IsNumeric(args[1].ToString()))
                                                                AAState.rCalc.SpinAngle = Convert.ToDouble(args[1]);

                                                        }
                                                        else if (args.Length == 3)
                                                        {
                                                            if (args[0].Substring(0, 1).ToLower() == "a")
                                                                AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolArithmetic;
                                                            if (Globals.IsNumeric(args[1].ToString()))
                                                                AAState.rCalc.SpinAngle = Convert.ToDouble(args[1]);
                                                            AAState.rCalc.UseDiameter = true;
                                                            AAState.rCalc.DiameterFieldName = args[2].ToString();

                                                        }
                                                        else
                                                        {
                                                            AAState.rCalc.RotationType = esriSymbolRotationType.esriRotateSymbolGeographic;
                                                        }

                                                    }
                                                    AAState.WriteLine("                  " + AAState.rCalc.RotationType.ToString() + " is being used");
                                                    rotationAngle = AAState.rCalc.GetRotationUsingConnectedEdges(inFeature);
                                                    if (rotationAngle == null)
                                                    {
                                                        AAState.WriteLine("                  Rotation Angle not found or errored out");
                                                        continue;
                                                    }

                                                    //Accept optional second argument to provide extra rotation

                                                    if (rotationAngle != -1)
                                                    {
                                                        AAState.WriteLine("                  Rotation Angle set to: " + rotationAngle.ToString());

                                                        inObject.set_Value(intFldIdxs[0], rotationAngle);
                                                        AAState.WriteLine("                  Rotation Angle set");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: JUNCTION_ROTATION \r\n" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: JUNCTION_ROTATION");
                                            }
                                            break;

                                        //For Release: 1.2
                                        //New Dynamic Value Method: Length - stores calculated length of line feature
                                        case "LENGTH":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: LENGTH");
                                                if (inFeature != null)
                                                {
                                                    curve = (ICurve)inFeature.Shape;
                                                    if (curve != null)
                                                    {
                                                        inObject.set_Value(intFldIdxs[0], curve.Length);
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: LENGTH: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: LENGTH");
                                            }
                                            break;

                                        //Release: 1.2
                                        //New Dynamic Value Method: SET_MEASURES - stores calculated M values (from 0 to length of line) for line feature 
                                        //Value Data options:
                                        //P = Percent - Ms will be zero to 100
                                        //default - Ms will be zero to length of line
                                        case "SET_MEASURES":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: SET_MEASURES");
                                                if (inFeature != null)
                                                {
                                                    curve = inFeature.Shape as ICurve;
                                                    mseg = inFeature.Shape as IMSegmentation;
                                                    if (curve != null && mseg != null)
                                                        if (valData != null && valData != "" && valData.Substring(0, 1).ToUpper() == "P")
                                                            mseg.SetAndInterpolateMsBetween(0, 100);
                                                        else
                                                            mseg.SetAndInterpolateMsBetween(0, curve.Length);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: SET_MEASURES: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: SET_MEASURES");
                                            }
                                            break;
                                        //case "EDGE_INTERSECT_SECOND":
                                        //    try
                                        //    {
                                        //        if (inFeature != null)
                                        //        {
                                        //            netFeat = inFeature as INetworkFeature;
                                        //            if (netFeat != null)
                                        //            {
                                        //                if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                        //                {

                                        //                    iJuncFeat = (IJunctionFeature)netFeat;
                                        //                    // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                        //                    ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                        //                    if (iSJunc == null)
                                        //                        break;
                                        //                    if (iSJunc.EdgeFeatureCount <= 1)
                                        //                        break;
                                        //                    iEdgeFeat = iSJunc.get_EdgeFeature(1);


                                        //                    // verify that field (in junction) to copy exists

                                        //                    IRow pRow = iEdgeFeat as IRow;

                                        //                    juncField = pRow.Fields.FindField(valData as string);
                                        //                    if (juncField > -1)
                                        //                    {
                                        //                        inObject.set_Value(intFldIdxs[0], pRow.get_Value(juncField));
                                        //                    }
                                        //                }
                                        //            }
                                        //        }
                                        //    }
                                        //    catch
                                        //    {
                                        //    }
                                        //    break;
                                        //case "EDGE_INTERSECT_FIRST":
                                        //    try
                                        //    {
                                        //        if (inFeature != null)
                                        //        {
                                        //            netFeat = inFeature as INetworkFeature;
                                        //            if (netFeat != null)
                                        //            {
                                        //                if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                        //                {

                                        //                    iJuncFeat = (IJunctionFeature)netFeat;
                                        //                    // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                        //                    ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                        //                    if (iSJunc == null)
                                        //                        break;
                                        //                    if (iSJunc.EdgeFeatureCount <= 0)
                                        //                        break;
                                        //                    iEdgeFeat = iSJunc.get_EdgeFeature(0);


                                        //                    IRow pRow = iEdgeFeat as IRow;

                                        //                    juncField = pRow.Fields.FindField(valData as string);
                                        //                    if (juncField > -1)
                                        //                    {
                                        //                        inObject.set_Value(intFldIdxs[0], pRow.get_Value(juncField));
                                        //                    }
                                        //                }
                                        //            }
                                        //        }
                                        //    }
                                        //    catch
                                        //    {
                                        //    }
                                        //    break;


                                        //Release: 2.0
                                        //New Dynamic Value Method: TO_EDGE_FIELD transfers a field value from a connected egde feature to a junction feature
                                        //Takes value from the frist edge whose "TO" point connects with this junction
                                        case "TO_EDGE_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: TO_EDGE_FIELD");
                                                if (inFeature != null)
                                                {
                                                    if (valData == null)
                                                    {
                                                        AAState.WriteLine("                  Value info is null");
                                                        break;
                                                    }
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {
                                                            string netField = "";
                                                            string netRestrictFC = "";
                                                            string netRestrictField = "";
                                                            string netRestrictValue = "";
                                                            args = valData.Split('|');
                                                            switch (args.GetLength(0))
                                                            {
                                                                case 1:  // sequenceColumnName only
                                                                    netField = args[0].ToString().Trim();
                                                                    break;
                                                                case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    break;
                                                                case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    break;
                                                                case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    netRestrictValue = args[3].ToString().Trim();
                                                                    break;
                                                                default: break;
                                                            }


                                                            iJuncFeat = (IJunctionFeature)netFeat;
                                                            // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount > 0)
                                                            {
                                                                for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                {
                                                                    iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                    try
                                                                    {

                                                                        if (netRestrictFC != "")
                                                                        {
                                                                            string strClsName = Globals.getClassName(((IDataset)((IFeature)iEdgeFeat).Class));

                                                                            AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                            if (strClsName != netRestrictFC)
                                                                            {
                                                                                AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                                continue;
                                                                            }
                                                                            AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                            if (netRestrictField != "" && netRestrictValue != "")
                                                                            {
                                                                                int intTmpFld = Globals.GetFieldIndex(((IFeature)iEdgeFeat).Fields, netRestrictField);

                                                                                if (intTmpFld > -1)
                                                                                {
                                                                                    //IFeature pTest = ((IFeature)iEdgeFeat);

                                                                                    if (((IFeature)iEdgeFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                                    {
                                                                                        AAState.WriteLine("                  Values Match");
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  Values dont match Match");
                                                                                        continue;

                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Missing Field restriction and value info");
                                                                            }
                                                                        }


                                                                        iJuncFeat = (IJunctionFeature)iEdgeFeat.FromJunctionFeature;
                                                                        if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                        {
                                                                            IRow pRow = iEdgeFeat as IRow;

                                                                            // verify that field (in junction) to copy exists
                                                                            juncField = Globals.GetFieldIndex(pRow.Fields, netField);

                                                                            if (juncField > -1)
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], pRow.get_Value(juncField));
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  " + netField + " field not found in edge");
                                                                            }
                                                                            pRow = null;
                                                                            break;


                                                                        }
                                                                    }
                                                                    catch
                                                                    {
                                                                        AAState.WriteLine("                  error ");
                                                                    }


                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  No Connected Edges Found");
                                                            }
                                                            iSJunc = null;
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_EDGE_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_EDGE_FIELD");
                                            }
                                            break;

                                        //Release: 2.0
                                        //New Dynamic Value Method: FROM_EDGE_FIELD transfers a field value from a connected egde feature to a junction feature
                                        //Takes value from the frist edge whose "FROM" point connects with this junction
                                        case "FROM_EDGE_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: FROM_EDGE_FIELD");
                                                if (inFeature != null)
                                                {
                                                    if (valData == null)
                                                    {
                                                        AAState.WriteLine("                  Value info is null");
                                                        break;
                                                    }
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {
                                                            string netField = "";
                                                            string netRestrictFC = "";
                                                            string netRestrictField = "";
                                                            string netRestrictValue = "";
                                                            args = valData.Split('|');
                                                            switch (args.GetLength(0))
                                                            {
                                                                case 1:  // sequenceColumnName only
                                                                    netField = args[0].ToString().Trim();
                                                                    break;
                                                                case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    break;
                                                                case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    break;
                                                                case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    netRestrictValue = args[3].ToString().Trim();
                                                                    break;
                                                                default: break;
                                                            }


                                                            iJuncFeat = (IJunctionFeature)netFeat;
                                                            // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount > 0)
                                                            {
                                                                for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                {
                                                                    iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                    try
                                                                    {
                                                                        if (netRestrictFC != "")
                                                                        {
                                                                            string strClsName = Globals.getClassName(((IDataset)((IFeature)iEdgeFeat).Class));

                                                                            AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                            if (strClsName != netRestrictFC)
                                                                            {
                                                                                AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                                continue;
                                                                            }
                                                                            AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                            if (netRestrictField != "" && netRestrictValue != "")
                                                                            {
                                                                                int intTmpFld = Globals.GetFieldIndex(((IFeature)iEdgeFeat).Fields, netRestrictField);

                                                                                if (intTmpFld > -1)
                                                                                {
                                                                                    //IFeature pTest = ((IFeature)iEdgeFeat);

                                                                                    if (((IFeature)iEdgeFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                                    {
                                                                                        AAState.WriteLine("                  Values Match");
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  Values dont match Match");
                                                                                        continue;

                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Missing Field restriction and value info");
                                                                            }
                                                                        }



                                                                        iJuncFeat = iEdgeFeat.ToJunctionFeature;
                                                                        if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                        {
                                                                            IRow pRow = iEdgeFeat as IRow;

                                                                            // verify that field (in junction) to copy exists
                                                                            juncField = Globals.GetFieldIndex(pRow.Fields, netField);

                                                                            if (juncField > -1)
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], pRow.get_Value(juncField));
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  " + iSJunc + " field not found");
                                                                            }
                                                                            pRow = null;

                                                                            break;


                                                                        }
                                                                    }
                                                                    catch
                                                                    {
                                                                        AAState.WriteLine("                  error ");
                                                                    }

                                                                }

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  No Connected Edges Found");
                                                            }
                                                            iSJunc = null;
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: FROM_EDGE_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: FROM_EDGE_FIELD");
                                            }
                                            break;

                                        case "TO_EDGE_STATS":
                                            try
                                            {
                                                if (valData == null) break;
                                                args = valData.Split('|');
                                                string statType = "MAX";
                                                switch (args.GetLength(0))
                                                {
                                                    case 1:
                                                        sourceFieldName = args[0].ToString();
                                                        break;
                                                    case 2:
                                                        sourceFieldName = args[0].ToString();
                                                        statType = args[1].ToString();
                                                        break;

                                                    default: break;
                                                }


                                                AAState.WriteLine("                  Trying: TO_EDGE_STATS");

                                                int AverageCount = 0;
                                                double result = -999999.1;
                                                string textRes = "";
                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        AAState.WriteLine("                  Feature is a network feature");
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {
                                                            AAState.WriteLine("                  Feature is a junction feature");
                                                            iJuncFeat = (IJunctionFeature)netFeat;
                                                            // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount > 0)
                                                            {
                                                                for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                {
                                                                    AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);

                                                                    iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                    try
                                                                    {
                                                                        AAState.WriteLine("                  Checking if Edge is From or To");

                                                                        iJuncFeat = iEdgeFeat.FromJunctionFeature;

                                                                        if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                        {
                                                                            AAState.WriteLine("                  Edge is a To-Edge");

                                                                            IRow pRow = iEdgeFeat as IRow;

                                                                            // verify that field (in junction) to copy exists
                                                                            AAState.WriteLine("                  Getting Field From To-Edge");
                                                                            juncField = pRow.Fields.FindField(sourceFieldName);
                                                                            string test = pRow.get_Value(juncField).ToString();
                                                                            if (Globals.IsNumeric(test))
                                                                            {

                                                                                double valToTest = Convert.ToDouble(test);
                                                                                if (result == -999999.1)
                                                                                {
                                                                                    result = valToTest;


                                                                                }
                                                                                else
                                                                                {
                                                                                    switch (statType.ToUpper())
                                                                                    {
                                                                                        case "MAX":
                                                                                            if (result < valToTest)
                                                                                            {
                                                                                                result = valToTest;

                                                                                            }
                                                                                            break;
                                                                                        case "MIN":
                                                                                            if (result > valToTest)
                                                                                            {
                                                                                                result = valToTest;

                                                                                            }
                                                                                            break;
                                                                                        case "SUM":
                                                                                            result += valToTest;


                                                                                            break;
                                                                                        case "AVERAGE":
                                                                                            result += valToTest;
                                                                                            AverageCount++;
                                                                                            break;
                                                                                        case "MEAN":
                                                                                            result += valToTest;
                                                                                            AverageCount++;

                                                                                            break;
                                                                                        case "CONCAT":
                                                                                            if (textRes.Contains(valToTest.ToString() + ConcatDelim))
                                                                                            {
                                                                                            }
                                                                                            else if (textRes.Contains(ConcatDelim + valToTest.ToString()))
                                                                                            {
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (textRes == "")
                                                                                                {
                                                                                                    textRes += valToTest.ToString();
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    textRes += ConcatDelim + valToTest.ToString();
                                                                                                }
                                                                                            }


                                                                                            break;
                                                                                        default:
                                                                                            AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                            break;
                                                                                    }

                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                switch (statType.ToUpper())
                                                                                {

                                                                                    case "CONCAT":
                                                                                        if (textRes.Contains(test.ToString() + ConcatDelim))
                                                                                        {
                                                                                        }
                                                                                        else if (textRes.Contains(ConcatDelim + test.ToString()))
                                                                                        {
                                                                                        }
                                                                                        else
                                                                                        {



                                                                                            if (textRes == "")
                                                                                            {
                                                                                                textRes += test;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                textRes += ConcatDelim + test;
                                                                                            }
                                                                                        }


                                                                                        break;
                                                                                    default:
                                                                                        AAState.WriteLine("                  ERROR/WARNING: Non numeric value returned: " + test);

                                                                                        AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                        break;
                                                                                }

                                                                            }


                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Checking if Edge is From Edge, skipping");

                                                                        }
                                                                    }
                                                                    catch
                                                                    {
                                                                    }
                                                                }//end loop
                                                                try
                                                                {
                                                                    if (textRes != "")
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], textRes);
                                                                    }
                                                                    else if (result != -999999.1)
                                                                    {
                                                                        if (AverageCount != 0)
                                                                        {
                                                                            result = result / AverageCount;
                                                                        }
                                                                        inObject.set_Value(intFldIdxs[0], result);

                                                                    }
                                                                    else
                                                                    {
                                                                        IField field = inObject.Fields.get_Field(intFldIdxs[0]);
                                                                        object newval = field.DefaultValue;
                                                                        if (newval == null)
                                                                        {
                                                                            if (field.IsNullable)
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], null);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            inObject.set_Value(intFldIdxs[0], newval);
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: Value was not set on feature: " + ex.Message);

                                                                }

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  No Connected Edges Found");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_EDGE_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_EDGE_STATS");
                                            }
                                            break;

                                        case "FROM_EDGE_STATS":
                                            try
                                            {
                                                if (valData == null) break;
                                                args = valData.Split('|');
                                                string statType = "MAX";
                                                switch (args.GetLength(0))
                                                {
                                                    case 1:
                                                        sourceFieldName = args[0].ToString();
                                                        break;
                                                    case 2:
                                                        sourceFieldName = args[0].ToString();
                                                        statType = args[1].ToString();
                                                        break;

                                                    default: break;
                                                }


                                                AAState.WriteLine("                  Trying: FROM_EDGE_STATS");

                                                int AverageCount = 0;
                                                double result = -999999.1;
                                                string textRes = "";
                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        AAState.WriteLine("                  Feature is a network feature");
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {
                                                            AAState.WriteLine("                  Feature is a junction feature");
                                                            iJuncFeat = (IJunctionFeature)netFeat;
                                                            // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount > 0)
                                                            {
                                                                for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                {
                                                                    AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);

                                                                    iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                    try
                                                                    {
                                                                        AAState.WriteLine("                  Checking if Edge is From or To");

                                                                        iJuncFeat = iEdgeFeat.ToJunctionFeature;

                                                                        if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                        {
                                                                            AAState.WriteLine("                  Edge is a From-Edge");

                                                                            IRow pRow = iEdgeFeat as IRow;

                                                                            // verify that field (in junction) to copy exists
                                                                            AAState.WriteLine("                  Getting Field From From-Edge");
                                                                            juncField = pRow.Fields.FindField(sourceFieldName);
                                                                            string test = pRow.get_Value(juncField).ToString();
                                                                            if (Globals.IsNumeric(test))
                                                                            {

                                                                                double valToTest = Convert.ToDouble(test);
                                                                                if (result == -999999.1)
                                                                                {
                                                                                    result = valToTest;


                                                                                }
                                                                                else
                                                                                {
                                                                                    switch (statType.ToUpper())
                                                                                    {
                                                                                        case "MAX":
                                                                                            if (result < valToTest)
                                                                                            {
                                                                                                result = valToTest;

                                                                                            }
                                                                                            break;
                                                                                        case "MIN":
                                                                                            if (result > valToTest)
                                                                                            {
                                                                                                result = valToTest;

                                                                                            }
                                                                                            break;
                                                                                        case "SUM":
                                                                                            result += valToTest;


                                                                                            break;
                                                                                        case "AVERAGE":
                                                                                            result += valToTest;
                                                                                            AverageCount++;
                                                                                            break;
                                                                                        case "MEAN":
                                                                                            result += valToTest;
                                                                                            AverageCount++;

                                                                                            break;
                                                                                        case "CONCAT":
                                                                                            if (textRes.Contains(valToTest.ToString() + ConcatDelim))
                                                                                            {
                                                                                            }
                                                                                            else if (textRes.Contains(ConcatDelim + valToTest.ToString()))
                                                                                            {
                                                                                            }
                                                                                            else
                                                                                            {

                                                                                                if (textRes == "")
                                                                                                {
                                                                                                    textRes += valToTest.ToString();
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    textRes += ConcatDelim + valToTest.ToString();
                                                                                                }

                                                                                            }

                                                                                            break;
                                                                                        default:
                                                                                            AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                            break;
                                                                                    }

                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                switch (statType.ToUpper())
                                                                                {

                                                                                    case "CONCAT":
                                                                                        if (textRes.Contains(test.ToString() + ConcatDelim))
                                                                                        {
                                                                                        }
                                                                                        else if (textRes.Contains(ConcatDelim + test.ToString()))
                                                                                        {
                                                                                        }
                                                                                        else
                                                                                        {

                                                                                            if (textRes == "")
                                                                                            {
                                                                                                textRes += test;
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                textRes += ConcatDelim + test;
                                                                                            }

                                                                                        }

                                                                                        break;
                                                                                    default:
                                                                                        AAState.WriteLine("                  ERROR/WARNING: Non numeric value returned: " + test);

                                                                                        AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                        break;
                                                                                }

                                                                            }


                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Checking if Edge is To Edge, skipping");

                                                                        }
                                                                    }
                                                                    catch
                                                                    {
                                                                    }
                                                                }//end loop
                                                                try
                                                                {
                                                                    if (textRes != "")
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], textRes);
                                                                    }
                                                                    else if (result != -999999.1)
                                                                    {
                                                                        if (AverageCount != 0)
                                                                        {
                                                                            result = result / AverageCount;
                                                                        }
                                                                        inObject.set_Value(intFldIdxs[0], result);

                                                                    }
                                                                    else
                                                                    {
                                                                        IField field = inObject.Fields.get_Field(intFldIdxs[0]);
                                                                        object newval = field.DefaultValue;
                                                                        if (newval == null)
                                                                        {
                                                                            if (field.IsNullable)
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], null);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            inObject.set_Value(intFldIdxs[0], newval);
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: Value was not set on feature: " + ex.Message);

                                                                }

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  No Connected Edges Found");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_EDGE_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_EDGE_STATS");
                                            }
                                            break;

                                        case "EDGE_STATS":
                                            try
                                            {
                                                if (valData == null) break;
                                                args = valData.Split('|');
                                                string statType = "MAX";
                                                switch (args.GetLength(0))
                                                {
                                                    case 1:
                                                        sourceFieldName = args[0].ToString();
                                                        break;
                                                    case 2:
                                                        sourceFieldName = args[0].ToString();
                                                        statType = args[1].ToString();
                                                        break;

                                                    default: break;
                                                }


                                                AAState.WriteLine("                  Trying: EDGE_STATS");

                                                int AverageCount = 0;
                                                double result = -999999.1;
                                                string textRes = "";
                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        AAState.WriteLine("                  Feature is a network feature");
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {
                                                            AAState.WriteLine("                  Feature is a junction feature");
                                                            iJuncFeat = (IJunctionFeature)netFeat;
                                                            // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount > 0)
                                                            {
                                                                for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                {
                                                                    AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);

                                                                    iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                    try
                                                                    {


                                                                        IRow pRow = iEdgeFeat as IRow;

                                                                        // verify that field (in junction) to copy exists
                                                                        AAState.WriteLine("                  Getting Field From Edge");
                                                                        juncField = pRow.Fields.FindField(sourceFieldName);
                                                                        string test = pRow.get_Value(juncField).ToString();
                                                                        if (Globals.IsNumeric(test))
                                                                        {

                                                                            double valToTest = Convert.ToDouble(test);
                                                                            if (result == -999999.1)
                                                                            {
                                                                                result = valToTest;


                                                                            }
                                                                            else
                                                                            {
                                                                                switch (statType.ToUpper())
                                                                                {
                                                                                    case "MAX":
                                                                                        if (result < valToTest)
                                                                                        {
                                                                                            result = valToTest;

                                                                                        }
                                                                                        break;
                                                                                    case "MIN":
                                                                                        if (result > valToTest)
                                                                                        {
                                                                                            result = valToTest;

                                                                                        }
                                                                                        break;
                                                                                    case "SUM":
                                                                                        result += valToTest;


                                                                                        break;
                                                                                    case "AVERAGE":
                                                                                        result += valToTest;
                                                                                        AverageCount++;
                                                                                        break;
                                                                                    case "MEAN":
                                                                                        result += valToTest;
                                                                                        AverageCount++;

                                                                                        break;
                                                                                    case "CONCAT":
                                                                                        if (textRes.Contains(valToTest.ToString() + ConcatDelim))
                                                                                        {
                                                                                        }
                                                                                        else if (textRes.Contains(ConcatDelim + valToTest.ToString()))
                                                                                        {
                                                                                        }
                                                                                        else
                                                                                        {

                                                                                            if (textRes == "")
                                                                                            {
                                                                                                textRes += valToTest.ToString();
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                textRes += ConcatDelim + valToTest.ToString();
                                                                                            }

                                                                                        }

                                                                                        break;
                                                                                    default:
                                                                                        AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                        break;
                                                                                }

                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            switch (statType.ToUpper())
                                                                            {

                                                                                case "CONCAT":
                                                                                    if (textRes.Contains(test.ToString() + ConcatDelim))
                                                                                    {
                                                                                    }
                                                                                    else if (textRes.Contains(ConcatDelim + test.ToString()))
                                                                                    {
                                                                                    }
                                                                                    else
                                                                                    {

                                                                                        if (textRes == "")
                                                                                        {
                                                                                            textRes += test;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            textRes += ConcatDelim + test;
                                                                                        }
                                                                                    }


                                                                                    break;
                                                                                default:
                                                                                    AAState.WriteLine("                  ERROR/WARNING: Non numeric value returned: " + test);

                                                                                    AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                    break;
                                                                            }

                                                                        }



                                                                    }
                                                                    catch
                                                                    {
                                                                    }
                                                                }//end loop
                                                                try
                                                                {
                                                                    if (textRes != "")
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], textRes);
                                                                    }
                                                                    else if (result != -999999.1)
                                                                    {
                                                                        if (AverageCount != 0)
                                                                        {
                                                                            result = result / AverageCount;
                                                                        }
                                                                        inObject.set_Value(intFldIdxs[0], result);

                                                                    }
                                                                    else
                                                                    {
                                                                        IField field = inObject.Fields.get_Field(intFldIdxs[0]);
                                                                        object newval = field.DefaultValue;
                                                                        if (newval == null)
                                                                        {
                                                                            if (field.IsNullable)
                                                                            {
                                                                                inObject.set_Value(intFldIdxs[0], null);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            inObject.set_Value(intFldIdxs[0], newval);
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: Value was not set on feature: " + ex.Message);

                                                                }

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  No Connected Edges Found");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_EDGE_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_EDGE_STATS");
                                            }
                                            break;
                                        case "TO_EDGE_MULTI_FIELD_INTERSECT":
                                            try
                                            {
                                                if (valData == null) break;

                                                sourceFieldName = "";
                                                sourceField = -1;
                                                found = false;
                                                //LayerToIntersect|Field To Elevate
                                                // Parse arguments
                                                args = valData.Split('|');
                                                int popFldIdx = 0;
                                                if (args.GetLength(0) >= 2)
                                                {
                                                    AAState.WriteLine("                  Parsing Valueinfo");


                                                    sourceFieldName = args[0].ToString();
                                                    string[] fieldsToPop = args[1].ToString().Split(',');

                                                    AAState.WriteLine("                  Trying: TO_EDGE_MULTI_FIELD_INTERSECT");


                                                    if (inFeature != null)
                                                    {
                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            AAState.WriteLine("                  Feature is a network feature");
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                            {
                                                                AAState.WriteLine("                  Feature is a junction feature");
                                                                iJuncFeat = (IJunctionFeature)netFeat;
                                                                // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                                ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                                if (iSJunc == null)
                                                                    break;
                                                                AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);
                                                                if (iSJunc.EdgeFeatureCount <= 0)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount > 0)
                                                                {
                                                                    for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                    {
                                                                        AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);

                                                                        iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                        try
                                                                        {
                                                                            AAState.WriteLine("                  Checking if Edge is From or To");

                                                                            iJuncFeat = iEdgeFeat.FromJunctionFeature;

                                                                            if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                            {
                                                                                AAState.WriteLine("                  Edge is a To-Edge");

                                                                                IRow pRow = iEdgeFeat as IRow;

                                                                                // verify that field (in junction) to copy exists
                                                                                AAState.WriteLine("                  Getting Field From To-Edge");
                                                                                juncField = pRow.Fields.FindField(sourceFieldName);
                                                                                string test = pRow.get_Value(juncField).ToString();
                                                                                if (fieldsToPop.Length == popFldIdx)
                                                                                    break;


                                                                                int tempFieldNum = inObject.Fields.FindField(fieldsToPop[popFldIdx]);

                                                                                if (tempFieldNum > -1)
                                                                                {
                                                                                    inObject.set_Value(tempFieldNum, test);
                                                                                    popFldIdx++;
                                                                                }


                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Checking if Edge is From Edge, skipping");

                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                        }
                                                                    }//end loop


                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  No Connected Edges Found");
                                                                }

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  not an junction feature");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Not a Geometric Network Feature");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_EDGE_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_EDGE_STATS");
                                            }
                                            break;
                                        case "FROM_EDGE_MULTI_FIELD_INTERSECT":
                                            try
                                            {
                                                if (valData == null) break;
                                                sourceFieldName = "";
                                                sourceField = -1;
                                                found = false;
                                                //LayerToIntersect|Field To Elevate
                                                // Parse arguments
                                                args = valData.Split('|');
                                                int popFldIdx = 0;
                                                if (args.GetLength(0) >= 2)
                                                {
                                                    AAState.WriteLine("                  Parsing Valueinfo");

                                                    sourceFieldName = args[0].ToString();
                                                    string[] fieldsToPop = args[1].ToString().Split(',');

                                                    AAState.WriteLine("                  Trying: FROM_EDGE_MULTI_FIELD_INTERSECT");


                                                    if (inFeature != null)
                                                    {
                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            AAState.WriteLine("                  Feature is a network feature");
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                            {
                                                                AAState.WriteLine("                  Feature is a junction feature");
                                                                iJuncFeat = (IJunctionFeature)netFeat;
                                                                // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                                ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                                if (iSJunc == null)
                                                                    break;
                                                                AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);
                                                                if (iSJunc.EdgeFeatureCount <= 0)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount > 0)
                                                                {
                                                                    for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                    {
                                                                        AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);

                                                                        iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                        try
                                                                        {
                                                                            AAState.WriteLine("                  Checking if Edge is From or To");

                                                                            iJuncFeat = iEdgeFeat.ToJunctionFeature;

                                                                            if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                            {
                                                                                AAState.WriteLine("                  Edge is a From-Edge");

                                                                                IRow pRow = iEdgeFeat as IRow;

                                                                                // verify that field (in junction) to copy exists
                                                                                AAState.WriteLine("                  Getting Field From From-Edge");
                                                                                juncField = pRow.Fields.FindField(sourceFieldName);
                                                                                string test = pRow.get_Value(juncField).ToString();
                                                                                if (fieldsToPop.Length == popFldIdx)
                                                                                    break;


                                                                                int tempFieldNum = inObject.Fields.FindField(fieldsToPop[popFldIdx]);

                                                                                if (tempFieldNum > -1)
                                                                                {
                                                                                    inObject.set_Value(tempFieldNum, test);
                                                                                    popFldIdx++;
                                                                                }


                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Checking if Edge is To Edge, skipping");

                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                        }
                                                                    }//end loop


                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  No Connected Edges Found");
                                                                }

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  not an junction feature");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Not a Geometric Network Feature");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_EDGE_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_EDGE_STATS");
                                            }
                                            break;

                                        case "FROM_JUNCTION_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: FROM_JUNCTION_FIELD");
                                                if (valData == null)
                                                {
                                                    AAState.WriteLine("                  Value info is null");
                                                    break;
                                                }
                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                        {
                                                            string netField = "";
                                                            string netRestrictFC = "";
                                                            string netRestrictField = "";
                                                            string netRestrictValue = "";
                                                            args = valData.Split('|');
                                                            switch (args.GetLength(0))
                                                            {
                                                                case 1:  // sequenceColumnName only
                                                                    netField = args[0].ToString().Trim();
                                                                    break;
                                                                case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    break;
                                                                case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    break;
                                                                case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    netRestrictValue = args[3].ToString().Trim();
                                                                    break;
                                                                default: break;
                                                            }

                                                            iEdgeFeat = (IEdgeFeature)netFeat;
                                                            iJuncFeat = iEdgeFeat.FromJunctionFeature;
                                                            if (netRestrictFC != "")
                                                            {
                                                                string strClsName = Globals.getClassName(((IDataset)((IFeature)iJuncFeat).Class));

                                                                AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                if (strClsName != netRestrictFC)
                                                                {
                                                                    AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                    break;
                                                                }
                                                                AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                if (netRestrictField != "" && netRestrictValue != "")
                                                                {
                                                                    int intTmpFld = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netRestrictField);

                                                                    if (intTmpFld > -1)
                                                                    {
                                                                        if (((IFeature)iJuncFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                        {
                                                                            AAState.WriteLine("                  Values Match");
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Values dont match Match");
                                                                            break;

                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  Missing Field restriction and value info");
                                                                }
                                                            }
                                                            // verify that field (in junction) to copy exists
                                                            juncField = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netField);
                                                            if (juncField > -1)
                                                            {
                                                                inObject.set_Value(intFldIdxs[0], ((IFeature)iJuncFeat).get_Value(juncField));
                                                            }
                                                            else
                                                                AAState.WriteLine("                  " + netField + " field not found");
                                                        }
                                                        else
                                                            AAState.WriteLine("                  not an edge feature");
                                                    }
                                                    else
                                                        AAState.WriteLine("                  Not a network feature");
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: FROM_JUNCTION_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: FROM_JUNCTION_FIELD");
                                            }
                                            break;


                                        //Release: 1.2
                                        //New Dynamic Value Method: TO_JUNCTION_FIELD transfers a field value from a junction connected at terminal end of a line feature
                                        case "TO_JUNCTION_FIELD":
                                            try
                                            {


                                                AAState.WriteLine("                  Trying: TO_JUNCTION_FIELD");
                                                if (valData == null)
                                                {
                                                    AAState.WriteLine("                  Value info is null");
                                                    break;
                                                }
                                                if (inFeature != null)
                                                {

                                                    netFeat = inFeature as INetworkFeature;
                                                    if (netFeat != null)
                                                    {
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                        {

                                                            string netField = "";
                                                            string netRestrictFC = "";
                                                            string netRestrictField = "";
                                                            string netRestrictValue = "";
                                                            args = valData.Split('|');
                                                            switch (args.GetLength(0))
                                                            {
                                                                case 1:  // sequenceColumnName only
                                                                    netField = args[0].ToString().Trim();
                                                                    break;
                                                                case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    break;
                                                                case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    break;
                                                                case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                    netField = args[0].ToString().Trim();
                                                                    netRestrictFC = args[1].ToString().Trim();
                                                                    netRestrictField = args[2].ToString().Trim();
                                                                    netRestrictValue = args[3].ToString().Trim();
                                                                    break;
                                                                default: break;
                                                            }


                                                            iEdgeFeat = (IEdgeFeature)netFeat;
                                                            iJuncFeat = iEdgeFeat.ToJunctionFeature;

                                                            if (netRestrictFC != "")
                                                            {
                                                                string strClsName = Globals.getClassName(((IDataset)((IFeature)iJuncFeat).Class));

                                                                AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                if (strClsName != netRestrictFC)
                                                                {
                                                                    AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                    break;
                                                                }
                                                                AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                if (netRestrictField != "" && netRestrictValue != "")
                                                                {
                                                                    int intTmpFld = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netRestrictField);

                                                                    if (intTmpFld > -1)
                                                                    {
                                                                        if (((IFeature)iJuncFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                        {
                                                                            AAState.WriteLine("                  Values Match");
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Values dont match Match");
                                                                            break;

                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  Missing Field restriction and value info");
                                                                }
                                                            }
                                                            // verify that field (in junction) to copy exists
                                                            juncField = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netField);

                                                            //juncField = ((IFeature)iJuncFeat).Fields.FindField(valData as string);
                                                            if (juncField > -1)
                                                            {
                                                                inObject.set_Value(intFldIdxs[0], ((IFeature)iJuncFeat).get_Value(juncField));
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  " + netField + " field not found");
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  not an edge feature");
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  not an geometric network feature");
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TO_JUNCTION_FIELD:" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TO_JUNCTION_FIELD");
                                            }
                                            break;
                                        case "UPDATE_TO_JUNCTION_FIELD":
                                            try
                                            {


                                                AAState.WriteLine("                  Trying: UPDATE_TO_JUNCTION_FIELD");
                                                IRowChanges pRowCh = null;

                                                pRowCh = inObject as IRowChanges;

                                                if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                {


                                                    if (valData == null)
                                                    {
                                                        AAState.WriteLine("                  Value info is null");
                                                        break;
                                                    }
                                                    if (inFeature != null)
                                                    {

                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                            {

                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:  // sequenceColumnName only
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }


                                                                iEdgeFeat = (IEdgeFeature)netFeat;
                                                                iJuncFeat = iEdgeFeat.ToJunctionFeature;

                                                                if (netRestrictFC != "")
                                                                {
                                                                    string strClsName = Globals.getClassName(((IDataset)((IFeature)iJuncFeat).Class));

                                                                    AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                    if (strClsName != netRestrictFC)
                                                                    {
                                                                        AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                        break;
                                                                    }
                                                                    AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                    if (netRestrictField != "" && netRestrictValue != "")
                                                                    {
                                                                        int intTmpFld = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netRestrictField);

                                                                        if (intTmpFld > -1)
                                                                        {
                                                                            if (((IFeature)iJuncFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                            {
                                                                                AAState.WriteLine("                  Values Match");
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Values dont match Match");
                                                                                break;

                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  Missing Field restriction and value info");
                                                                    }
                                                                }
                                                                // verify that field (in junction) to copy exists
                                                                juncField = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netField);

                                                                //juncField = ((IFeature)iJuncFeat).Fields.FindField(valData as string);
                                                                if (juncField > -1)
                                                                {
                                                                    if (ChangeFeatureList == null)
                                                                    {
                                                                        ChangeFeatureList = new List<IObject>();
                                                                    }
                                                                    ((IFeature)iJuncFeat).set_Value(juncField, inObject.get_Value(intFldIdxs[0]));
                                                                    ChangeFeatureList.Add(((IFeature)iJuncFeat));

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  " + netField + " field not found");
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an edge feature");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  not an geometric network feature");
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  field was not changed");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: UPDATE_TO_JUNCTION_FIELD:" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: UPDATE_TO_JUNCTION_FIELD");
                                            }
                                            break;
                                        case "UPDATE_FROM_JUNCTION_FIELD":
                                            try
                                            {


                                                AAState.WriteLine("                  Trying: UPDATE_FROM_JUNCTION_FIELD");
                                                IRowChanges pRowCh = null;

                                                pRowCh = inObject as IRowChanges;

                                                if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                {

                                                    if (valData == null)
                                                    {
                                                        AAState.WriteLine("                  Value info is null");
                                                        break;
                                                    }
                                                    if (inFeature != null)
                                                    {

                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                            {

                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }





                                                                iEdgeFeat = (IEdgeFeature)netFeat;
                                                                iJuncFeat = iEdgeFeat.FromJunctionFeature;

                                                                if (netRestrictFC != "")
                                                                {
                                                                    string strClsName = Globals.getClassName(((IDataset)((IFeature)iJuncFeat).Class));

                                                                    AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                    if (strClsName != netRestrictFC)
                                                                    {
                                                                        AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                        break;
                                                                    }
                                                                    AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                    if (netRestrictField != "" && netRestrictValue != "")
                                                                    {
                                                                        int intTmpFld = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netRestrictField);

                                                                        if (intTmpFld > -1)
                                                                        {
                                                                            if (((IFeature)iJuncFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                            {
                                                                                AAState.WriteLine("                  Values Match");
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Values dont match Match");
                                                                                break;

                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  Missing Field restriction and value info");
                                                                    }
                                                                }
                                                                // verify that field (in junction) to copy exists
                                                                juncField = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netField);

                                                                //juncField = ((IFeature)iJuncFeat).Fields.FindField(valData as string);
                                                                if (juncField > -1)
                                                                {
                                                                    if (ChangeFeatureList == null)
                                                                    {
                                                                        ChangeFeatureList = new List<IObject>();
                                                                    }
                                                                    ((IFeature)iJuncFeat).set_Value(juncField, inObject.get_Value(intFldIdxs[0]));
                                                                    ChangeFeatureList.Add(((IFeature)iJuncFeat));

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  " + netField + " field not found");
                                                                }
                                                            }


                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an edge feature");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  not an geometric network feature");
                                                    }

                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  field was not changed");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: UPDATE_FROM_JUNCTION_FIELD:" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: UPDATE_FROM_JUNCTION_FIELD");
                                            }
                                            break;
                                        case "UPDATE_FROM_EDGE_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: UPDATE_FROM_EDGE_FIELD");
                                                if (inFeature != null)
                                                {
                                                    IRowChanges pRowCh = null;

                                                    pRowCh = inObject as IRowChanges;

                                                    if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                    {

                                                        if (valData == null)
                                                        {
                                                            AAState.WriteLine("                  Value info is null");
                                                            break;
                                                        }
                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                            {
                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:  // sequenceColumnName only
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }


                                                                iJuncFeat = (IJunctionFeature)netFeat;
                                                                // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                                ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                                if (iSJunc == null)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount <= 0)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount > 0)
                                                                {
                                                                    for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                    {
                                                                        iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                        try
                                                                        {
                                                                            if (netRestrictFC != "")
                                                                            {
                                                                                string strClsName = Globals.getClassName(((IDataset)((IFeature)iEdgeFeat).Class));

                                                                                AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                                if (strClsName != netRestrictFC)
                                                                                {
                                                                                    AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                                    continue;
                                                                                }
                                                                                AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                                if (netRestrictField != "" && netRestrictValue != "")
                                                                                {
                                                                                    int intTmpFld = Globals.GetFieldIndex(((IFeature)iEdgeFeat).Fields, netRestrictField);

                                                                                    if (intTmpFld > -1)
                                                                                    {
                                                                                        //IFeature pTest = ((IFeature)iEdgeFeat);

                                                                                        if (((IFeature)iEdgeFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                                        {
                                                                                            AAState.WriteLine("                  Values Match");
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            AAState.WriteLine("                  Values dont match Match");
                                                                                            continue;

                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  Missing Field restriction and value info");
                                                                                }
                                                                            }



                                                                            iJuncFeat = iEdgeFeat.ToJunctionFeature;
                                                                            if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                            {
                                                                                IFeature pRow = iEdgeFeat as IFeature;

                                                                                // verify that field (in junction) to copy exists
                                                                                juncField = Globals.GetFieldIndex(pRow.Fields, netField);

                                                                                if (juncField > -1)
                                                                                {
                                                                                    if (ChangeFeatureList == null)
                                                                                    {
                                                                                        ChangeFeatureList = new List<IObject>();
                                                                                    }
                                                                                    pRow.set_Value(juncField, inObject.get_Value(intFldIdxs[0]));
                                                                                    ChangeFeatureList.Add(((IFeature)pRow));

                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  " + iSJunc + " field not found");
                                                                                }
                                                                                pRow = null;

                                                                                break;


                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                            AAState.WriteLine("                  error ");
                                                                        }

                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  No Connected Edges Found");
                                                                }
                                                                iSJunc = null;
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  not an junction feature");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Not a Geometric Network Feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  field was not changed");
                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: UPDATE_FROM_EDGE_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: UPDATE_FROM_EDGE_FIELD");
                                            }
                                            break;
                                        case "UPDATE_TO_EDGE_FIELD":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: UPDATE_TO_EDGE_FIELD");
                                                if (inFeature != null)
                                                {
                                                    IRowChanges pRowCh = null;

                                                    pRowCh = inObject as IRowChanges;

                                                    if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                    {

                                                        if (valData == null)
                                                        {
                                                            AAState.WriteLine("                  Value info is null");
                                                            break;
                                                        }
                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                            {
                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:  // sequenceColumnName only
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }


                                                                iJuncFeat = (IJunctionFeature)netFeat;
                                                                // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                                ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                                if (iSJunc == null)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount <= 0)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount > 0)
                                                                {
                                                                    for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                    {
                                                                        iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                        try
                                                                        {

                                                                            if (netRestrictFC != "")
                                                                            {
                                                                                string strClsName = Globals.getClassName(((IDataset)((IFeature)iEdgeFeat).Class));

                                                                                AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                                if (strClsName != netRestrictFC)
                                                                                {
                                                                                    AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                                    continue;
                                                                                }
                                                                                AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                                if (netRestrictField != "" && netRestrictValue != "")
                                                                                {
                                                                                    int intTmpFld = Globals.GetFieldIndex(((IFeature)iEdgeFeat).Fields, netRestrictField);

                                                                                    if (intTmpFld > -1)
                                                                                    {
                                                                                        //IFeature pTest = ((IFeature)iEdgeFeat);

                                                                                        if (((IFeature)iEdgeFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                                        {
                                                                                            AAState.WriteLine("                  Values Match");
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            AAState.WriteLine("                  Values dont match Match");
                                                                                            continue;

                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  Missing Field restriction and value info");
                                                                                }
                                                                            }


                                                                            iJuncFeat = (IJunctionFeature)iEdgeFeat.FromJunctionFeature;
                                                                            if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                            {
                                                                                IFeature pRow = iEdgeFeat as IFeature;

                                                                                // verify that field (in junction) to copy exists
                                                                                juncField = Globals.GetFieldIndex(pRow.Fields, netField);

                                                                                if (juncField > -1)
                                                                                {
                                                                                    if (ChangeFeatureList == null)
                                                                                    {
                                                                                        ChangeFeatureList = new List<IObject>();
                                                                                    }
                                                                                    pRow.set_Value(juncField, inObject.get_Value(intFldIdxs[0]));
                                                                                    ChangeFeatureList.Add(((IFeature)pRow));

                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  " + netField + " field not found in edge");
                                                                                }
                                                                                pRow = null;
                                                                                break;


                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                            AAState.WriteLine("                  error ");
                                                                        }


                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  No Connected Edges Found");
                                                                }
                                                                iSJunc = null;
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  not an junction feature");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Not a Geometric Network Feature");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  field was not changed");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: UPDATE_TO_EDGE_FIELD: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: UPDATE_TO_EDGE_FIELD");
                                            }
                                            break;
                                        //***************8











                                        case "TRIGGER_UPDATE_INTERSECTING_FEATURE"://Intersected Feature|FieldIntersectingFeatureToChange|FromFieldinModifiedFeature
                                            {
                                                try
                                                {
                                                    IFeatureCursor fLocalCursor = null;
                                                    IFeature sourceFeatureLocal = null;
                                                    AAState.WriteLine("                  Trying: TRIGGER_UPDATE_INTERSECTING_FEATURE");
                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 3)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    if (inFeature == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: The input features is null");
                                                        break;
                                                    }
                                                    bool cont = true;
                                                    if (intFldIdxs.Count > 1)
                                                    {
                                                        IRowChanges inChanges = inObject as IRowChanges;
                                                        if (inChanges.get_ValueChanged(intFldIdxs[0]))
                                                        {
                                                            cont = true;
                                                        }
                                                        else
                                                        {
                                                            cont = false;
                                                        }


                                                        inChanges = null;

                                                    }
                                                    if (cont)
                                                    {


                                                        sourceLayerName = "";
                                                        sourceFieldName = "";
                                                        sourceField = -1;
                                                        found = false;
                                                        AAState.WriteLine("                  Getting Value Info");
                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString().Trim();
                                                        string targetFieldName = args[2].ToString().Trim();
                                                        AAState.WriteLine("                  Checking values");
                                                        if (sourceFieldName != null)
                                                        {
                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                sourceLayerName = sourceLayerNames[i].ToString().Trim();
                                                                if (sourceLayerName != "")
                                                                {

                                                                    boolLayerOrFC = true;
                                                                    if (sourceLayerName.Contains("("))
                                                                    {
                                                                        string[] tempSplt = sourceLayerName.Split('(');
                                                                        sourceLayerName = tempSplt[0].Trim();
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                    }
                                                                    if (sourceLayer != null)
                                                                    {

                                                                        if (Globals.IsEditable(ref sourceLayer, ref AAState._editor))
                                                                        {


                                                                            if (inObject.Class.ObjectClassID != sourceLayer.FeatureClass.ObjectClassID)
                                                                            {

                                                                                sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, false);

                                                                                pFS = (IFeatureSelection)sourceLayer;
                                                                                if (boolLayerOrFC)
                                                                                {
                                                                                    if (pFS.SelectionSet.Count > 0)
                                                                                    {
                                                                                        pFS.SelectionSet.Search(sFilter, false, out cCurs);
                                                                                        fLocalCursor = cCurs as IFeatureCursor;


                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        fLocalCursor = sourceLayer.Search(sFilter, false);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    fLocalCursor = sourceLayer.FeatureClass.Search(sFilter, false);
                                                                                }



                                                                                while ((sourceFeatureLocal = fLocalCursor.NextFeature()) != null)
                                                                                {
                                                                                    try
                                                                                    {
                                                                                        if (sourceFeatureLocal.Class.ObjectClassID != inFeature.Class.ObjectClassID)
                                                                                        {
                                                                                            if (sourceFieldName == "CREATE")
                                                                                            {
                                                                                                if (NewFeatureList == null)
                                                                                                {
                                                                                                    NewFeatureList = new List<IObject>();
                                                                                                }

                                                                                                NewFeatureList.Add(((IFeature)sourceFeatureLocal));
                                                                                            }
                                                                                            else if (sourceFieldName == "CHANGEGEO")
                                                                                            {
                                                                                                if (ChangeFeatureGeoList == null)
                                                                                                {
                                                                                                    ChangeFeatureGeoList = new List<IObject>();
                                                                                                }

                                                                                                ChangeFeatureGeoList.Add(((IFeature)sourceFeatureLocal));
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (ChangeFeatureList == null)
                                                                                                {
                                                                                                    ChangeFeatureList = new List<IObject>();
                                                                                                }

                                                                                                ChangeFeatureList.Add(((IFeature)sourceFeatureLocal));
                                                                                            }


                                                                                        }
                                                                                        else if (sourceFeatureLocal.Class == inFeature.Class && sourceFeatureLocal.OID != inFeature.OID)
                                                                                        {
                                                                                            if (sourceFieldName == "CREATE")
                                                                                            {
                                                                                                if (NewFeatureList == null)
                                                                                                {
                                                                                                    NewFeatureList = new List<IObject>();
                                                                                                }

                                                                                                NewFeatureList.Add(((IFeature)sourceFeatureLocal));
                                                                                            }
                                                                                            else if (sourceFieldName == "CHANGEGEO")
                                                                                            {
                                                                                                if (ChangeFeatureGeoList == null)
                                                                                                {
                                                                                                    ChangeFeatureGeoList = new List<IObject>();
                                                                                                }

                                                                                                ChangeFeatureGeoList.Add(((IFeature)sourceFeatureLocal));
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (ChangeFeatureList == null)
                                                                                                {
                                                                                                    ChangeFeatureList = new List<IObject>();
                                                                                                }

                                                                                                ChangeFeatureList.Add(((IFeature)sourceFeatureLocal));
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR setting value");
                                                                                    }
                                                                                    finally
                                                                                    {

                                                                                    }


                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer is not editable: " + sourceLayerName);
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                }
                                                            }
                                                        }
                                                        if (found)
                                                        {
                                                            break;

                                                        }



                                                    }
                                                    fLocalCursor = null;
                                                    sourceFeatureLocal = null;
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: TRIGGER_UPDATE_INTERSECTING_FEATURE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: TRIGGER_UPDATE_INTERSECTING_FEATURE");

                                                }
                                                break;

                                            }



                                        case "TRIGGER_UPDATE_TO_JUNCTION":
                                            try
                                            {


                                                AAState.WriteLine("                  Trying: TRIGGER_UPDATE_TO_JUNCTION");
                                                IRowChanges pRowCh = null;

                                                pRowCh = inObject as IRowChanges;

                                                if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                {


                                                    if (valData == null)
                                                    {
                                                        AAState.WriteLine("                  Value info is null");
                                                        break;
                                                    }
                                                    if (inFeature != null)
                                                    {

                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                            {

                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:  // sequenceColumnName only
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }


                                                                iEdgeFeat = (IEdgeFeature)netFeat;
                                                                iJuncFeat = iEdgeFeat.ToJunctionFeature;

                                                                if (netRestrictFC != "")
                                                                {
                                                                    string strClsName = Globals.getClassName(((IDataset)((IFeature)iJuncFeat).Class));

                                                                    AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                    if (strClsName != netRestrictFC)
                                                                    {
                                                                        AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                        break;
                                                                    }
                                                                    AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                    if (netRestrictField != "" && netRestrictValue != "")
                                                                    {
                                                                        int intTmpFld = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netRestrictField);

                                                                        if (intTmpFld > -1)
                                                                        {
                                                                            if (((IFeature)iJuncFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                            {
                                                                                AAState.WriteLine("                  Values Match");
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Values dont match Match");
                                                                                break;

                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  Missing Field restriction and value info");
                                                                    }
                                                                }
                                                                // verify that field (in junction) to copy exists
                                                                if (netField == "CREATE")
                                                                {
                                                                    if (NewFeatureList == null)
                                                                    {
                                                                        NewFeatureList = new List<IObject>();
                                                                    }

                                                                    NewFeatureList.Add(((IFeature)iJuncFeat));
                                                                }
                                                                else if (netField == "CHANGEGEO")
                                                                {
                                                                    if (ChangeFeatureGeoList == null)
                                                                    {
                                                                        ChangeFeatureGeoList = new List<IObject>();
                                                                    }

                                                                    ChangeFeatureGeoList.Add(((IFeature)iJuncFeat));
                                                                }
                                                                else
                                                                {
                                                                    if (ChangeFeatureList == null)
                                                                    {
                                                                        ChangeFeatureList = new List<IObject>();
                                                                    }

                                                                    ChangeFeatureList.Add(((IFeature)iJuncFeat));
                                                                }

                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an edge feature");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  not an geometric network feature");
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  field was not changed");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TRIGGER_UPDATE_TO_JUNCTION:" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TRIGGER_UPDATE_TO_JUNCTION");
                                            }
                                            break;
                                        case "TRIGGER_UPDATE_FROM_JUNCTION":
                                            try
                                            {


                                                AAState.WriteLine("                  Trying: TRIGGER_UPDATE_FROM_JUNCTION");
                                                IRowChanges pRowCh = null;

                                                pRowCh = inObject as IRowChanges;

                                                if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                {

                                                    if (valData == null)
                                                    {
                                                        AAState.WriteLine("                  Value info is null");
                                                        break;
                                                    }
                                                    if (inFeature != null)
                                                    {

                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexEdge || inFeature.FeatureType == esriFeatureType.esriFTSimpleEdge)
                                                            {

                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }





                                                                iEdgeFeat = (IEdgeFeature)netFeat;
                                                                iJuncFeat = iEdgeFeat.FromJunctionFeature;

                                                                if (netRestrictFC != "")
                                                                {
                                                                    string strClsName = Globals.getClassName(((IDataset)((IFeature)iJuncFeat).Class));

                                                                    AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                    if (strClsName != netRestrictFC)
                                                                    {
                                                                        AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                        break;
                                                                    }
                                                                    AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                    if (netRestrictField != "" && netRestrictValue != "")
                                                                    {
                                                                        int intTmpFld = Globals.GetFieldIndex(((IFeature)iJuncFeat).Fields, netRestrictField);

                                                                        if (intTmpFld > -1)
                                                                        {
                                                                            if (((IFeature)iJuncFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                            {
                                                                                AAState.WriteLine("                  Values Match");
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Values dont match Match");
                                                                                break;

                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  Missing Field restriction and value info");
                                                                    }
                                                                }
                                                                if (netField == "CREATE")
                                                                {
                                                                    if (NewFeatureList == null)
                                                                    {
                                                                        NewFeatureList = new List<IObject>();
                                                                    }

                                                                    NewFeatureList.Add(((IFeature)iJuncFeat));
                                                                }
                                                                else if (netField == "CHANGEGEO")
                                                                {
                                                                    if (ChangeFeatureGeoList == null)
                                                                    {
                                                                        ChangeFeatureGeoList = new List<IObject>();
                                                                    }

                                                                    ChangeFeatureGeoList.Add(((IFeature)iJuncFeat));
                                                                }
                                                                else
                                                                {
                                                                    if (ChangeFeatureList == null)
                                                                    {
                                                                        ChangeFeatureList = new List<IObject>();
                                                                    }

                                                                    ChangeFeatureList.Add(((IFeature)iJuncFeat));
                                                                }
                                                            }


                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an edge feature");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  not an geometric network feature");
                                                    }

                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  field was not changed");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TRIGGER_UPDATE_FROM_JUNCTION:" + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TRIGGER_UPDATE_FROM_JUNCTION");
                                            }
                                            break;
                                        case "TRIGGER_UPDATE_FROM_EDGE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: TRIGGER_UPDATE_FROM_EDGE");
                                                if (inFeature != null)
                                                {
                                                    IRowChanges pRowCh = null;

                                                    pRowCh = inObject as IRowChanges;

                                                    if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                    {

                                                        if (valData == null)
                                                        {
                                                            AAState.WriteLine("                  Value info is null");
                                                            break;
                                                        }
                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                            {
                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:  // sequenceColumnName only
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }


                                                                iJuncFeat = (IJunctionFeature)netFeat;
                                                                // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                                ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                                if (iSJunc == null)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount <= 0)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount > 0)
                                                                {
                                                                    for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                    {
                                                                        iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                        try
                                                                        {
                                                                            if (netRestrictFC != "")
                                                                            {
                                                                                string strClsName = Globals.getClassName(((IDataset)((IFeature)iEdgeFeat).Class));

                                                                                AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                                if (strClsName != netRestrictFC)
                                                                                {
                                                                                    AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                                    continue;
                                                                                }
                                                                                AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                                if (netRestrictField != "" && netRestrictValue != "")
                                                                                {
                                                                                    int intTmpFld = Globals.GetFieldIndex(((IFeature)iEdgeFeat).Fields, netRestrictField);

                                                                                    if (intTmpFld > -1)
                                                                                    {
                                                                                        //IFeature pTest = ((IFeature)iEdgeFeat);

                                                                                        if (((IFeature)iEdgeFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                                        {
                                                                                            AAState.WriteLine("                  Values Match");
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            AAState.WriteLine("                  Values dont match Match");
                                                                                            continue;

                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  Missing Field restriction and value info");
                                                                                }
                                                                            }



                                                                            iJuncFeat = iEdgeFeat.ToJunctionFeature;
                                                                            if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                            {

                                                                                if (netField == "CREATE")
                                                                                {
                                                                                    if (NewFeatureList == null)
                                                                                    {
                                                                                        NewFeatureList = new List<IObject>();
                                                                                    }

                                                                                    NewFeatureList.Add(((IFeature)iEdgeFeat));
                                                                                }
                                                                                else if (netField == "CHANGEGEO")
                                                                                {
                                                                                    if (ChangeFeatureGeoList == null)
                                                                                    {
                                                                                        ChangeFeatureGeoList = new List<IObject>();
                                                                                    }

                                                                                    ChangeFeatureGeoList.Add(((IFeature)iEdgeFeat));
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (ChangeFeatureList == null)
                                                                                    {
                                                                                        ChangeFeatureList = new List<IObject>();
                                                                                    }

                                                                                    ChangeFeatureList.Add(((IFeature)iEdgeFeat));
                                                                                }

                                                                                break;


                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                            AAState.WriteLine("                  error ");
                                                                        }

                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  No Connected Edges Found");
                                                                }
                                                                iSJunc = null;
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  not an junction feature");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Not a Geometric Network Feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  field was not changed");
                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TRIGGER_UPDATE_FROM_EDGE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TRIGGER_UPDATE_FROM_EDGE");
                                            }
                                            break;
                                        case "TRIGGER_UPDATE_TO_EDGE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: TRIGGER_UPDATE_TO_EDGE");
                                                if (inFeature != null)
                                                {
                                                    IRowChanges pRowCh = null;

                                                    pRowCh = inObject as IRowChanges;

                                                    if (pRowCh.get_ValueChanged(intFldIdxs[0]) == true)
                                                    {

                                                        if (valData == null)
                                                        {
                                                            AAState.WriteLine("                  Value info is null");
                                                            break;
                                                        }
                                                        netFeat = inFeature as INetworkFeature;
                                                        if (netFeat != null)
                                                        {
                                                            if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                            {
                                                                string netField = "";
                                                                string netRestrictFC = "";
                                                                string netRestrictField = "";
                                                                string netRestrictValue = "";
                                                                args = valData.Split('|');
                                                                switch (args.GetLength(0))
                                                                {
                                                                    case 1:  // sequenceColumnName only
                                                                        netField = args[0].ToString().Trim();
                                                                        break;
                                                                    case 2:  // sequenceColumnName|sequenceFixedWidth
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        break;
                                                                    case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        break;
                                                                    case 4:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                                        netField = args[0].ToString().Trim();
                                                                        netRestrictFC = args[1].ToString().Trim();
                                                                        netRestrictField = args[2].ToString().Trim();
                                                                        netRestrictValue = args[3].ToString().Trim();
                                                                        break;
                                                                    default: break;
                                                                }


                                                                iJuncFeat = (IJunctionFeature)netFeat;
                                                                // IComplexJunctionFeature iCEd = iJuncFeat as IComplexJunctionFeature;
                                                                ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                                if (iSJunc == null)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount <= 0)
                                                                    break;
                                                                if (iSJunc.EdgeFeatureCount > 0)
                                                                {
                                                                    for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                    {
                                                                        iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                        try
                                                                        {

                                                                            if (netRestrictFC != "")
                                                                            {
                                                                                string strClsName = Globals.getClassName(((IDataset)((IFeature)iEdgeFeat).Class));

                                                                                AAState.WriteLine("                  Layer restriction found, Class name of junction is: " + strClsName);
                                                                                if (strClsName != netRestrictFC)
                                                                                {
                                                                                    AAState.WriteLine("                  Layer restriction does not match, breaking");
                                                                                    continue;
                                                                                }
                                                                                AAState.WriteLine("                  Layer restriction matches, checking for field restriction");
                                                                                if (netRestrictField != "" && netRestrictValue != "")
                                                                                {
                                                                                    int intTmpFld = Globals.GetFieldIndex(((IFeature)iEdgeFeat).Fields, netRestrictField);

                                                                                    if (intTmpFld > -1)
                                                                                    {
                                                                                        //IFeature pTest = ((IFeature)iEdgeFeat);

                                                                                        if (((IFeature)iEdgeFeat).get_Value(intTmpFld).ToString() == netRestrictValue)
                                                                                        {
                                                                                            AAState.WriteLine("                  Values Match");
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            AAState.WriteLine("                  Values dont match Match");
                                                                                            continue;

                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  " + netRestrictField + " was not found");
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  Missing Field restriction and value info");
                                                                                }
                                                                            }


                                                                            iJuncFeat = (IJunctionFeature)iEdgeFeat.FromJunctionFeature;
                                                                            if (((IFeature)iJuncFeat).Shape.Equals(inFeature.Shape))
                                                                            {
                                                                                if (netField == "CREATE")
                                                                                {
                                                                                    if (NewFeatureList == null)
                                                                                    {
                                                                                        NewFeatureList = new List<IObject>();
                                                                                    }

                                                                                    NewFeatureList.Add(((IFeature)iEdgeFeat));
                                                                                }
                                                                                else if (netField == "CHANGEGEO")
                                                                                {
                                                                                    if (ChangeFeatureGeoList == null)
                                                                                    {
                                                                                        ChangeFeatureGeoList = new List<IObject>();
                                                                                    }

                                                                                    ChangeFeatureGeoList.Add(((IFeature)iEdgeFeat));
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (ChangeFeatureList == null)
                                                                                    {
                                                                                        ChangeFeatureList = new List<IObject>();
                                                                                    }

                                                                                    ChangeFeatureList.Add(((IFeature)iEdgeFeat));
                                                                                }

                                                                                break;
                                                                                break;


                                                                            }
                                                                        }
                                                                        catch
                                                                        {
                                                                            AAState.WriteLine("                  error ");
                                                                        }


                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  No Connected Edges Found");
                                                                }
                                                                iSJunc = null;
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  not an junction feature");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  Not a Geometric Network Feature");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  field was not changed");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: TRIGGER_UPDATE_TO_EDGE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: TRIGGER_UPDATE_TO_EDGE");
                                            }
                                            break;
                                        //***********8
                                        //Release: 1.2
                                        //New Dynamic Value Method: GENERATE_ID - uses value in specificed table and increments it as specified
                                        case "GENERATE_ID":


                                            try
                                            {
                                                AAState.WriteLine("                  Trying: GENERATE_ID");
                                                if (AAState._gentab != null)
                                                {
                                                    sequenceColumnName = "";
                                                    sequenceColumnNum = -1;
                                                    sequenceValue = -1;
                                                    sequenceFixedWidth = "";
                                                    sequencePadding = 0;
                                                    formatString = "";

                                                    // Parse arguments
                                                    if (valData == null) break;
                                                    args = valData.Split('|');
                                                    switch (args.GetLength(0))
                                                    {
                                                        case 1:  // sequenceColumnName only
                                                            sequenceColumnName = args[0].ToString(); break;
                                                        case 2:  // sequenceColumnName|sequenceFixedWidth
                                                            sequenceColumnName = args[0].ToString();
                                                            sequenceFixedWidth = args[1].ToString();
                                                            break;
                                                        case 3:  // sequenceColumnName|sequenceFixedWidth|formatString
                                                            sequenceColumnName = args[0].ToString();
                                                            sequenceFixedWidth = args[1].ToString();
                                                            formatString = args[2].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    //Check for requested zero padding of sequence number
                                                    if (sequenceFixedWidth != "")
                                                        int.TryParse(sequenceFixedWidth.ToString(), out sequencePadding);
                                                    if (sequencePadding > 25)
                                                    {

                                                        AAState.WriteLine("                  WARNING: you are trying to pad your id with a more than 25 - 0's");
                                                        AAState.WriteLine("                  WARNING: " + sequencePadding + " 0's is what you have");

                                                    }
                                                    else if (sequencePadding > 50)
                                                    {
                                                        MessageBox.Show("You are trying to add 50 places to your ID, this is not supported, please fix your dynamic value table");
                                                    }
                                                    qFilter = new QueryFilterClass();
                                                    qFilter.WhereClause = "SEQNAME = '" + sequenceColumnName + "'";
                                                    cCurs = AAState._gentab.Update(qFilter, false);
                                                    sequenceColumnNum = AAState._gentab.Fields.FindField("SEQCOUNTER");
                                                    //get value of first row, increment it, and return incremented value
                                                    for (int j = 0; j < 51; j++)
                                                    {
                                                        row = cCurs.NextRow();
                                                        if (row == null)
                                                        {
                                                            break;
                                                        }
                                                        if (row.get_Value(sequenceColumnNum) == null)
                                                        {
                                                            sequenceValue = 0;
                                                        }
                                                        else if (row.get_Value(sequenceColumnNum).ToString() == "")
                                                        {
                                                            sequenceValue = 0;
                                                        }
                                                        else
                                                            sequenceValue = Convert.ToInt32(row.get_Value(sequenceColumnNum));


                                                        // _editEvents.OnChangeFeature -= OnChangeFeature;
                                                        // _editEvents.OnCreateFeature -= OnCreateFeature;
                                                        int sequenceInt = 1;

                                                        if (AAState._gentab.Fields.FindField("SEQINTERV") > 0)
                                                        {
                                                            if (row.get_Value(AAState._gentab.Fields.FindField("SEQINTERV")) != null)
                                                            {
                                                                if (row.get_Value(AAState._gentab.Fields.FindField("SEQINTERV")) != DBNull.Value)
                                                                    sequenceInt = Convert.ToInt32(row.get_Value(AAState._gentab.Fields.FindField("SEQINTERV")));
                                                            }
                                                        }
                                                        sequenceValue = sequenceValue + sequenceInt;

                                                        row.set_Value(sequenceColumnNum, sequenceValue);
                                                        AAState.WriteLine("                  " + row.Fields.get_Field(sequenceColumnNum).AliasName + " changed to " + sequenceValue);
                                                        //AAState.changeFeature -= OnChangeFeature;
                                                        //AAState.createFeature -= OnCreateFeature;

                                                        row.Store();
                                                        //AAState.changeFeature += OnChangeFeature;
                                                        //AAState.createFeature += OnCreateFeature;

                                                        //  _editEvents.OnChangeFeature += OnChangeFeature;
                                                        //  _editEvents.OnCreateFeature += OnCreateFeature;
                                                        if (Convert.ToInt32(row.get_Value(sequenceColumnNum)) == sequenceValue)
                                                            break;

                                                    }
                                                    if (sequenceValue == -1)
                                                    {
                                                        AAState.WriteLine("                  ERROR: GENERATE_ID: Sequence Not Found");
                                                    }

                                                    else
                                                    {
                                                        if (inObject.Fields.get_Field(intFldIdxs[0]).Type == esriFieldType.esriFieldTypeString)
                                                            if (formatString == null || formatString == "" || formatString.ToLower().IndexOf("[seq]") == -1)
                                                            {
                                                                string setVal = (sequenceValue.ToString("D" + sequencePadding) + sequencePostfix).ToString();

                                                                if (inObject.Fields.get_Field(intFldIdxs[0]).Length < setVal.Length && inObject.Fields.get_Field(intFldIdxs[0]).Length != 0)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sequenceValue + " is to long for field " + row.Fields.get_Field(sequenceColumnNum).AliasName);
                                                                }
                                                                else
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], (sequenceValue.ToString("D" + sequencePadding) + sequencePostfix).Trim());
                                                                    AAState.WriteLine("                  " + inObject.Fields.get_Field(intFldIdxs[0]).AliasName + " set to " + sequenceValue.ToString("D" + sequencePadding) + sequencePostfix);

                                                                }



                                                            }
                                                            else
                                                            {


                                                                int locIdx = formatString.ToUpper().IndexOf("[SEQ]");
                                                                if (locIdx >= 0)
                                                                {
                                                                    formatString = formatString.Remove(locIdx, 5);
                                                                    formatString = formatString.Insert(locIdx, sequenceValue.ToString("D" + sequencePadding));
                                                                }
                                                                //formatString = formatString.Replace("[seq]", sequenceValue.ToString("D" + sequencePadding));


                                                                if (inObject.Fields.get_Field(intFldIdxs[0]).Length < formatString.Length && inObject.Fields.get_Field(intFldIdxs[0]).Length != 0)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + formatString + " is to long for field " + row.Fields.get_Field(sequenceColumnNum).AliasName);
                                                                }
                                                                else
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], formatString.Trim());
                                                                    AAState.WriteLine("                  " + inObject.Fields.get_Field(intFldIdxs[0]).AliasName + " set to " + formatString);
                                                                }


                                                            }
                                                        else
                                                        {

                                                            inObject.set_Value(intFldIdxs[0], sequenceValue);
                                                            AAState.WriteLine("                  " + sequenceColumnNum + " changed to " + sequenceValue);
                                                        }
                                                    }

                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: GENERATE_ID table is not found");

                                                }

                                            }


                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: GENERATE_ID: " + ex.Message);
                                            }
                                            finally
                                            {
                                                if (cCurs != null)
                                                {
                                                    Marshal.ReleaseComObject(cCurs);
                                                    GC.Collect(300);
                                                    GC.WaitForFullGCComplete();
                                                    cCurs = null;

                                                }
                                                AAState.WriteLine("                  Finished: GENERATE_ID");
                                            }
                                            break;

                                        case "GENERATE_ID_BY_INTERSECT":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: GENERATE_ID_BY_INTERSECT");
                                                if (AAState._gentab != null && inFeature != null && !(inFeature.Shape.IsEmpty))
                                                {
                                                    sequenceColumnName = "";
                                                    sequenceColumnNum = -1;
                                                    sequenceValue = -1;
                                                    sequenceFixedWidth = "";
                                                    sequencePadding = 0;
                                                    //genIdAreaFieldName = "";
                                                    intersectLayerName = "";
                                                    intersectLayerFieldName = "";
                                                    formatString = "";
                                                    intersectFieldPos = -1;

                                                    // Parse arguments
                                                    if (valData == null) break;

                                                    args = valData.Split('|');
                                                    if (args.GetLength(0) < 3)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Improper value method");
                                                        break;
                                                    }
                                                    switch (args.GetLength(0))
                                                    {
                                                        case 3:  //columnName
                                                            intersectLayerName = args[0].ToString();
                                                            intersectLayerFieldName = args[1].ToString();
                                                            // genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[2].ToString();
                                                            break;
                                                        case 4:  // columnName|sequenceFixedWidth 
                                                            //sequenceFixedWidth formats the sequence with leading zeros to create specified width
                                                            intersectLayerName = args[0].ToString();
                                                            intersectLayerFieldName = args[1].ToString();
                                                            //genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[2].ToString();
                                                            sequenceFixedWidth = Convert.ToString(0);
                                                            formatString = args[3].ToString();

                                                            break;
                                                        case 5:  // columnName|sequenceFixedWidth|formatString 
                                                            //formatString must contain [seq] and [id]  and may contain [area] plus any desired text
                                                            intersectLayerName = args[0].ToString();
                                                            intersectLayerFieldName = args[1].ToString();
                                                            //genIdAreaFieldName = args[2].ToString();
                                                            sequenceColumnName = args[2].ToString();
                                                            sequenceFixedWidth = args[3].ToString();
                                                            formatString = args[4].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    //Find Area Layer
                                                    // FindLayerByName(areaLayerName, out areaLayer);

                                                    boolLayerOrFC = true;
                                                    if (intersectLayerName.Contains("("))
                                                    {
                                                        string[] tempSplt = intersectLayerName.Split('(');
                                                        intersectLayerName = tempSplt[0];
                                                        intersectLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, intersectLayerName, ref boolLayerOrFC);
                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                        {
                                                            boolLayerOrFC = true;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                        {
                                                            boolLayerOrFC = true;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                        {
                                                            boolLayerOrFC = false;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                        {
                                                            boolLayerOrFC = false;
                                                        }
                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                        {
                                                            boolLayerOrFC = false;
                                                        }
                                                        else
                                                        {
                                                            boolLayerOrFC = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        intersectLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, intersectLayerName, ref boolLayerOrFC);

                                                    }
                                                    if (intersectLayer == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Intersecting feature Layer(" + intersectLayerName + ") not found");
                                                        break;
                                                    }
                                                    //Find Area Field
                                                    intersectFieldPos = intersectLayer.FeatureClass.FindField(intersectLayerFieldName);
                                                    if (intersectFieldPos < 0)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Intersecting feature Layer Field(" + intersectLayerFieldName + ") not found");
                                                        break;
                                                    }


                                                    //Perform spatial search 
                                                    IGeometry pSearchGeo = Globals.GetGeomCenter((IGeometry)inFeature.ShapeCopy)[0];
                                                    pSearchGeo.SpatialReference = (inFeature.Class as IGeoDataset).SpatialReference;
                                                    pSearchGeo.Project((intersectLayer as IGeoDataset).SpatialReference);

                                                    sFilter = Globals.createSpatialFilter(intersectLayer, inFeature, false);

                                                    pFS = (IFeatureSelection)intersectLayer;
                                                    if (boolLayerOrFC)
                                                    {
                                                        if (pFS.SelectionSet.Count > 0)
                                                        {
                                                            pFS.SelectionSet.Search(sFilter, true, out cCurs);
                                                            fCursor = cCurs as IFeatureCursor;


                                                        }
                                                        else
                                                        {
                                                            fCursor = intersectLayer.Search(sFilter, true);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        fCursor = intersectLayer.FeatureClass.Search(sFilter, true);
                                                    }

                                                    sourceFeature = fCursor.NextFeature();
                                                    intersectValue = "-9999.1";
                                                    if (sourceFeature == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Intersecting feature not found");
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        while (sourceFeature != null)
                                                        {
                                                            if (sourceFeature.Class != inFeature.Class)
                                                            {
                                                                intersectValue = sourceFeature.get_Value(intersectFieldPos).ToString();
                                                                break;
                                                            }
                                                            else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                            {
                                                                intersectValue = sourceFeature.get_Value(intersectFieldPos).ToString();
                                                                break;
                                                            }
                                                            sourceFeature = fCursor.NextFeature();
                                                        }
                                                    }
                                                    if (intersectValue == "-9999.1")
                                                    {
                                                        AAState.WriteLine("                  ERROR: Intersecting feature not found");
                                                        break;
                                                    }
                                                    //Check for requested zero padding of sequence number
                                                    if (sequenceFixedWidth != "")
                                                        int.TryParse(sequenceFixedWidth.ToString(), out sequencePadding);

                                                    if (sequencePadding > 25)
                                                    {
                                                        AAState.WriteLine("                  WARNING: you are trying to pad your id with a more than 25 - 0's");
                                                        AAState.WriteLine("                  WARNING: " + sequencePadding + " 0's is what you have");

                                                    }
                                                    sequenceColumnName = sequenceColumnName + intersectValue;
                                                    AAState.WriteLine("                  Looking for a field called " + sequenceColumnName + " in the generate ID table");

                                                    qFilter = new QueryFilterClass();
                                                    qFilter.WhereClause = "SEQNAME = '" + sequenceColumnName + "'";
                                                    cCurs = AAState._gentab.Update(qFilter, false);
                                                    sequenceColumnNum = AAState._gentab.Fields.FindField("SEQCOUNTER");

                                                    //get value of first row, increment it, and return incremented value
                                                    for (int j = 0; j < 51; j++)
                                                    {
                                                        row = cCurs.NextRow();
                                                        if (row == null)
                                                        {
                                                            break;
                                                        }
                                                        if (row.get_Value(sequenceColumnNum) == null)
                                                        {
                                                            sequenceValue = 0;
                                                        }
                                                        else if (row.get_Value(sequenceColumnNum).ToString() == "")
                                                        {
                                                            sequenceValue = 0;
                                                        }
                                                        else
                                                            sequenceValue = Convert.ToInt32(row.get_Value(sequenceColumnNum));


                                                        int sequenceInt = 1;


                                                        if (AAState._gentab.Fields.FindField("SEQINTERV") > 0)
                                                        {
                                                            if (row.get_Value(AAState._gentab.Fields.FindField("SEQINTERV")) != null)
                                                            {
                                                                if (row.get_Value(AAState._gentab.Fields.FindField("SEQINTERV")) != DBNull.Value)
                                                                    sequenceInt = Convert.ToInt32(row.get_Value(AAState._gentab.Fields.FindField("SEQINTERV")));
                                                            }
                                                        }
                                                        sequenceValue = sequenceValue + sequenceInt;


                                                        row.set_Value(sequenceColumnNum, sequenceValue);

                                                        row.Store();

                                                        if (Convert.ToInt32(row.get_Value(sequenceColumnNum)) == sequenceValue)
                                                            break;

                                                    }
                                                    if (sequenceValue == -1)
                                                    {
                                                        AAState.WriteLine("                  ERROR: Sequence number not found");
                                                    }
                                                    else
                                                    {
                                                        if (inObject.Fields.get_Field(intFldIdxs[0]).Type == esriFieldType.esriFieldTypeString)
                                                            if (formatString == null || formatString == "" || (formatString.ToUpper().IndexOf("[SEQ]") == -1 && formatString.ToUpper().IndexOf("[ID]") == -1))
                                                                inObject.set_Value(intFldIdxs[0], intersectValue + sequenceValue.ToString("D" + sequencePadding) + sequencePostfix);
                                                            else
                                                            {

                                                                int locIdx = formatString.ToUpper().IndexOf("[ID]");
                                                                if (locIdx >= 0)
                                                                {
                                                                    formatString = formatString.Remove(locIdx, 4);
                                                                    formatString = formatString.Insert(locIdx, intersectValue);
                                                                }

                                                                locIdx = formatString.ToUpper().IndexOf("[SEQ]");
                                                                if (locIdx >= 0)
                                                                {
                                                                    string sequenceValuePad = sequenceValue.ToString("D" + sequencePadding);
                                                                    formatString = formatString.Remove(locIdx, 5);
                                                                    formatString = formatString.Insert(locIdx, sequenceValuePad.ToString());
                                                                }
                                                                //
                                                                inObject.set_Value(intFldIdxs[0], formatString);
                                                            }
                                                        else
                                                            inObject.set_Value(intFldIdxs[0], sequenceValue);
                                                    }


                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: GENERATE_ID table is not found");

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: GENERATE_ID_BY_INTERSECT: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: GENERATE_ID_BY_INTERSECT");
                                            }
                                            break;

                                        //Modified for Release 1.2  (No longer uses ICalculator)
                                        //Requires valid VBScript expression
                                        //Can include string, numeric, and date fields by name in square brackets [] 
                                        //Example: DateDiff("yyyy",[INSTALLDATE],Now()) 
                                        case "EXPRESSION":
                                            try
                                            {

                                                AAState.WriteLine("                  Trying: EXPRESSION");
                                                if (inObject != null & valData != null)
                                                {
                                                    int intTargetFld = -1;
                                                    if (intFldIdxs.Count == 0)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        intTargetFld = intFldIdxs[0];
                                                    }

                                                    newValue = valData;
                                                    for (int i = 0; i <= inObject.Fields.FieldCount; i++)
                                                    {

                                                        string strTmpFldName;
                                                        int intTmpIdx;
                                                        if (i == inObject.Fields.FieldCount)
                                                        {
                                                            testField = inObject.Fields.get_Field(intFldIdxs[0]);
                                                            strTmpFldName = "#";
                                                            intTmpIdx = intFldIdxs[0];
                                                        }
                                                        else
                                                        {
                                                            testField = inObject.Fields.get_Field(i);
                                                            strTmpFldName = testField.Name;
                                                            intTmpIdx = i;
                                                        }


                                                        int indFld = newValue.ToUpper().IndexOf("[" + strTmpFldName.ToUpper() + "]");
                                                        while (indFld >= 0)
                                                        {
                                                            AAState.WriteLine("                  replace field: " + testField.Name + " with a value");
                                                            int fldLen = strTmpFldName.Length;
                                                            string tmpStr1 = newValue.Substring(0, indFld + 1);
                                                            string tmpStr2 = newValue.Substring(indFld + fldLen + 1);
                                                            newValue = tmpStr1 + "_REPLACE_VAL_" + tmpStr2;

                                                            switch (testField.Type)
                                                            {
                                                                case esriFieldType.esriFieldTypeString:

                                                                    if (inObject.get_Value(intTmpIdx) == null || inObject.get_Value(intTmpIdx).ToString() == "")
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (newValue.Contains("isNull"))
                                                                        {
                                                                            newValue = newValue.Replace("isNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (newValue.Contains("ISNULL"))
                                                                        {
                                                                            newValue = newValue.Replace("ISNULL([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx) == null)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + "\"");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx) == DBNull.Value)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + "\"");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx).ToString() == "")
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(intTmpIdx).ToString() + "\"");
                                                                        }
                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(intTmpIdx).ToString() + "\"");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "False");
                                                                        }

                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(intTmpIdx).ToString() + "\"");
                                                                        }
                                                                    }



                                                                    break;
                                                                case esriFieldType.esriFieldTypeDate:


                                                                    if (inObject.get_Value(intTmpIdx) == null || inObject.get_Value(intTmpIdx).ToString() == "" || inObject.get_Value(intTmpIdx) == DBNull.Value)
                                                                    {
                                                                        if (newValue.Contains("IsNull([" + "_REPLACE_VAL_" + "])"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (newValue.Contains("isNull([" + "_REPLACE_VAL_" + "])"))
                                                                        {
                                                                            newValue = newValue.Replace("isNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (newValue.Contains("ISNULL([" + "_REPLACE_VAL_" + "])"))
                                                                        {
                                                                            newValue = newValue.Replace("ISNULL([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx) == null)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(intTmpIdx).ToString() + "\"");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx) == DBNull.Value)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + "\"");//"\"" + inObject.get_Value(intTmpIdx).ToString() + "\"");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx).ToString() == "")
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + inObject.get_Value(intTmpIdx).ToString() + "\"");
                                                                        }
                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "CDATE(\"" + inObject.get_Value(intTmpIdx).ToString() + "\")");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "False");
                                                                        }

                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "CDATE(\"" + inObject.get_Value(intTmpIdx).ToString() + "\")");
                                                                        }
                                                                    }




                                                                    break;

                                                                default:
                                                                    if (inObject.get_Value(intTmpIdx) == null || inObject.get_Value(intTmpIdx).ToString() == "")
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (newValue.Contains("isNull"))
                                                                        {
                                                                            newValue = newValue.Replace("isNull([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (newValue.Contains("ISNULL"))
                                                                        {
                                                                            newValue = newValue.Replace("ISNULL([" + "_REPLACE_VAL_" + "])", "True");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx) == null)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + "\"");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx) == DBNull.Value)
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + "\"");
                                                                        }
                                                                        else if (inObject.get_Value(intTmpIdx).ToString() == "")
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "\"" + "\"");
                                                                        }
                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", "" + inObject.get_Value(intTmpIdx).ToString() + "");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (newValue.Contains("IsNull"))
                                                                        {
                                                                            newValue = newValue.Replace("IsNull([" + "_REPLACE_VAL_" + "])", "False");
                                                                        }

                                                                        else
                                                                        {
                                                                            newValue = newValue.Replace("[" + "_REPLACE_VAL_" + "]", inObject.get_Value(intTmpIdx).ToString());
                                                                        }
                                                                    }

                                                                    break;
                                                            }
                                                            indFld = newValue.ToUpper().IndexOf("[" + testField.Name.ToUpper() + "]");
                                                        }
                                                    }

                                                    try
                                                    {
                                                        AAState.WriteLine("                  Checking to verify there is a field to store the expression");
                                                        if (intTargetFld > -1)
                                                        {
                                                            newValue = script.Eval(newValue).ToString();
                                                            if (newValue.ToUpper() == "<Null>".ToUpper())
                                                            {
                                                                AAState.WriteLine("                  Setting Null");
                                                                inObject.set_Value(intTargetFld, DBNull.Value);
                                                            }
                                                            else if (inObject.get_Value(intTargetFld).ToString() != newValue)
                                                            {
                                                                AAState.WriteLine("                  Setting Value to: " + newValue.Trim());
                                                                inObject.set_Value(intTargetFld, newValue.Trim());
                                                            }
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        AAState.WriteLine("                  ERROR: evaluating the expression for feature in " + inObject.Class.AliasName + " with OID of " + inObject.OID);
                                                        AAState.WriteLine("                         " + ex.Message);
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: EXPRESSION: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: EXPRESSION");
                                            }
                                            break;

                                        // GUID values are calculated into text fields or into native GUID field types
                                        // When using text field you have an optional argument (valdata) to specify the format
                                        // N-none 32 chars, D-dash 36, B-braces 38, P-Parenthesis 38
                                        case "GUID":
                                            try
                                            {
                                                if (inObject != null)
                                                {
                                                    if (inObject.Fields.get_Field(intFldIdxs[0]).Type == esriFieldType.esriFieldTypeGUID)
                                                        inObject.set_Value(intFldIdxs[0], System.Guid.NewGuid().ToString("B"));
                                                    else if (inObject.Fields.get_Field(intFldIdxs[0]).Type == esriFieldType.esriFieldTypeString &&
                                                             inObject.Fields.get_Field(intFldIdxs[0]).Length >= 32)
                                                    {

                                                        valData = valData.Trim();
                                                        if (valData != "N" && valData != "D" && valData != "B" && valData != "P")
                                                            if (inObject.Fields.get_Field(intFldIdxs[0]).Length >= 38)
                                                                valData = "B";  //Default to braces 
                                                            else if (inObject.Fields.get_Field(intFldIdxs[0]).Length < 36)
                                                                valData = "N";
                                                            else
                                                                valData = "D";
                                                        inObject.set_Value(intFldIdxs[0], System.Guid.NewGuid().ToString(valData));
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: EXPRESSION: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: EXPRESSION");
                                            }
                                            break;

                                        case "CREATE_LINKED_RECORD"://Feature Layer|Field To Copy|Field To Populate|Primary Key Field|Foreign Key Field
                                            {
                                                try
                                                {
                                                    AAState.WriteLine("                  Trying: CREATE_LINKED_RECORD");
                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length < 5)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    if (inObject == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: The input features is null");
                                                        break;
                                                    }
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    AAState.WriteLine("                  Getting Value Info");
                                                    sourceLayerNames = args[0].ToString().Split(',');
                                                    sourceFieldName = args[1].ToString().Trim();
                                                    string targetFieldName = args[2].ToString().Trim();
                                                    string sourceIDFieldName = args[3].ToString().Trim();
                                                    string targetIDFieldName = args[4].ToString().Trim();
                                                    int countFld = 1;
                                                    if (args.Length == 6)
                                                    {

                                                        if (!Globals.IsNumeric(args[5].ToString().Trim()))
                                                        {
                                                            int fldx = Globals.GetFieldIndex(inObject.Fields, args[5].ToString().Trim());

                                                            if (fldx > 0)
                                                            {
                                                                string tempVal = inObject.get_Value(fldx).ToString();
                                                                if (Globals.IsNumeric(tempVal))
                                                                    countFld = Convert.ToInt32(tempVal);


                                                            }

                                                        }
                                                        else
                                                        {
                                                            countFld = Convert.ToInt32(args[5].ToString().Trim());

                                                        }

                                                    }
                                                    AAState.WriteLine("                  Checking values");
                                                    if (sourceFieldName != null)
                                                    {
                                                        AAState.WriteLine("                  Checking Fields in Source Layer");
                                                        int fldValToCopyIdx = Globals.GetFieldIndex(inObject.Fields, sourceFieldName);
                                                        int fldIDToCopyIdx = Globals.GetFieldIndex(inObject.Fields, sourceIDFieldName);
                                                        if (fldValToCopyIdx > -1 && fldIDToCopyIdx > -1)
                                                        {

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                sourceLayerName = sourceLayerNames[i].ToString().Trim();

                                                                if (sourceLayerName != "")
                                                                {

                                                                    // Get layer
                                                                    AAState.WriteLine("                  Checking for table to populate");
                                                                    bool FCorLayerSource = true;
                                                                    sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref FCorLayerSource);

                                                                    if (sourceLayer != null)
                                                                    {
                                                                        AAState.WriteLine("                  " + sourceLayerName + " Was not found");

                                                                    }
                                                                    else
                                                                    {
                                                                        ITable pTable = Globals.FindTable(AAState._editor.Map, sourceLayerName);
                                                                        if (pTable != null)
                                                                        {
                                                                            int fldValToPopIdx = Globals.GetFieldIndex(pTable.Fields, targetFieldName);
                                                                            int fldIDToPopIdx = Globals.GetFieldIndex(pTable.Fields, targetIDFieldName);
                                                                            if (fldValToPopIdx > -1 && fldIDToPopIdx > -1)
                                                                            {
                                                                                AAState.WriteLine("                  Trying to create a row in the target table");
                                                                                IRow pNewRow;
                                                                                for (int j = 0; j < countFld; j++)
                                                                                {
                                                                                    pNewRow = pTable.CreateRow();
                                                                                    AAState.WriteLine("                  Row Created");
                                                                                    AAState.WriteLine("                  Trying to Copy ID");
                                                                                    try
                                                                                    {
                                                                                        pNewRow.set_Value(fldIDToPopIdx, inObject.get_Value(fldIDToCopyIdx));

                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(fldIDToCopyIdx) + " to field: " + targetIDFieldName);
                                                                                    }
                                                                                    AAState.WriteLine("                  ID successfully copied");
                                                                                    AAState.WriteLine("                  Trying to Copy Value");
                                                                                    try
                                                                                    {
                                                                                        pNewRow.set_Value(fldValToPopIdx, inObject.get_Value(fldValToCopyIdx));

                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        AAState.WriteLine("                  ERROR: Could not Copy: " + inObject.get_Value(fldValToCopyIdx) + " to field: " + targetFieldName);
                                                                                    }
                                                                                    if (NewFeatureList == null)
                                                                                    {
                                                                                        NewFeatureList = new List<IObject>();
                                                                                    }
                                                                                    IObject featobj = pNewRow as IObject;


                                                                                    if (featobj != null)
                                                                                    {
                                                                                        NewFeatureList.Add(featobj);
                                                                                    }

                                                                                    AAState.WriteLine("                  Row successfully stored");
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: ID or Field to populate was not found");
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR: Table to populate not found: " + sourceLayerName);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: ID or Field to Copy was not found");
                                                        }


                                                    }

                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: CREATE_LINKED_RECORD" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: CREATE_LINKED_RECORD");

                                                }
                                                break;

                                            }

                                        case "UPDATE_INTERSECTING_FEATURE"://Intersected Feature|FieldIntersectingFeatureToChange|FromFieldinModifiedFeature
                                            {
                                                try
                                                {
                                                    IFeatureCursor fLocalCursor = null;
                                                    IFeature sourceFeatureLocal = null;
                                                    AAState.WriteLine("                  Trying: UPDATE_INTERSECTING_FEATURE");
                                                    if (!String.IsNullOrEmpty(valData))
                                                    {
                                                        args = valData.Split('|');
                                                        if (args.Length != 3)
                                                        {
                                                            AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Format of valdata incorrect");
                                                        break;
                                                    }
                                                    if (inFeature == null)
                                                    {
                                                        AAState.WriteLine("                  ERROR: The input features is null");
                                                        break;
                                                    }
                                                    bool cont = true;
                                                    if (intFldIdxs.Count > 1)
                                                    {
                                                        IRowChanges inChanges = inObject as IRowChanges;
                                                        if (inChanges.get_ValueChanged(intFldIdxs[0]))
                                                        {
                                                            cont = true;
                                                        }
                                                        else
                                                        {
                                                            cont = false;
                                                        }


                                                        inChanges = null;

                                                    }
                                                    if (cont)
                                                    {


                                                        sourceLayerName = "";
                                                        sourceFieldName = "";
                                                        sourceField = -1;
                                                        found = false;
                                                        AAState.WriteLine("                  Getting Value Info");
                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString().Trim();
                                                        string targetFieldName = args[2].ToString().Trim();
                                                        AAState.WriteLine("                  Checking values");
                                                        if (sourceFieldName != null)
                                                        {
                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                sourceLayerName = sourceLayerNames[i].ToString().Trim();
                                                                if (sourceLayerName != "")
                                                                {

                                                                    boolLayerOrFC = true;
                                                                    if (sourceLayerName.Contains("("))
                                                                    {
                                                                        string[] tempSplt = sourceLayerName.Split('(');
                                                                        sourceLayerName = tempSplt[0].Trim();
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                    }
                                                                    if (sourceLayer != null)
                                                                    {

                                                                        if (Globals.IsEditable(ref sourceLayer, ref AAState._editor))
                                                                        {


                                                                            if (inObject.Class.ObjectClassID != sourceLayer.FeatureClass.ObjectClassID)
                                                                            {
                                                                                sourceField = Globals.GetFieldIndex(sourceLayer.FeatureClass.Fields, sourceFieldName);

                                                                                if (sourceField > -1)
                                                                                {
                                                                                    sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, false);


                                                                                    int fldIdx = Globals.GetFieldIndex(inFeature.Fields, targetFieldName);
                                                                                    AAState.WriteLine("                  " + targetFieldName + " at index " + fldIdx);
                                                                                    string test = targetFieldName;
                                                                                    if (fldIdx > -1)
                                                                                    {
                                                                                        test = inFeature.get_Value(fldIdx).ToString().Trim();
                                                                                        AAState.WriteLine("                  Value Found " + test);
                                                                                    }
                                                                                    AAState.WriteLine("                  Value used " + test);


                                                                                    pFS = (IFeatureSelection)sourceLayer;
                                                                                    if (boolLayerOrFC)
                                                                                    {
                                                                                        if (pFS.SelectionSet.Count > 0)
                                                                                        {
                                                                                            pFS.SelectionSet.Search(sFilter, false, out cCurs);
                                                                                            fLocalCursor = cCurs as IFeatureCursor;


                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            fLocalCursor = sourceLayer.Search(sFilter, false);
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        fLocalCursor = sourceLayer.FeatureClass.Search(sFilter, false);
                                                                                    }



                                                                                    while ((sourceFeatureLocal = fLocalCursor.NextFeature()) != null)
                                                                                    {
                                                                                        try
                                                                                        {
                                                                                            if (sourceFeatureLocal.Class.ObjectClassID != inFeature.Class.ObjectClassID)
                                                                                            {
                                                                                                if (sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeString)
                                                                                                {
                                                                                                    sourceFeatureLocal.set_Value(sourceField, test);

                                                                                                    if (ChangeFeatureList == null)
                                                                                                    {
                                                                                                        ChangeFeatureList = new List<IObject>();
                                                                                                    }
                                                                                                    ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                    found = true;

                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if (Globals.IsNumeric(test))
                                                                                                    {
                                                                                                        if (sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeSmallInteger ||
                                                                                                            sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeInteger)
                                                                                                        {

                                                                                                            sourceFeatureLocal.set_Value(sourceField, Convert.ToInt32(test));

                                                                                                            if (ChangeFeatureList == null)
                                                                                                            {
                                                                                                                ChangeFeatureList = new List<IObject>();
                                                                                                            }
                                                                                                            ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                            found = true;
                                                                                                            //break;
                                                                                                        }
                                                                                                        else if (sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeDouble ||
                                                                                                            sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeSingle)
                                                                                                        {
                                                                                                            sourceFeatureLocal.set_Value(sourceField, Convert.ToDouble(test));


                                                                                                            if (ChangeFeatureList == null)
                                                                                                            {
                                                                                                                ChangeFeatureList = new List<IObject>();
                                                                                                            }
                                                                                                            ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                            found = true;

                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            sourceFeatureLocal.set_Value(sourceField, test as object);

                                                                                                            if (ChangeFeatureList == null)
                                                                                                            {
                                                                                                                ChangeFeatureList = new List<IObject>();
                                                                                                            }
                                                                                                            ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                            found = true;

                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        sourceFeatureLocal.set_Value(sourceField, test as object);


                                                                                                        if (ChangeFeatureList == null)
                                                                                                        {
                                                                                                            ChangeFeatureList = new List<IObject>();
                                                                                                        }
                                                                                                        ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                        found = true;

                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                            else if (sourceFeatureLocal.Class == inFeature.Class && sourceFeatureLocal.OID != inFeature.OID)
                                                                                            {
                                                                                                if (sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeString)
                                                                                                {
                                                                                                    sourceFeatureLocal.set_Value(sourceField, test);


                                                                                                    if (ChangeFeatureList == null)
                                                                                                    {
                                                                                                        ChangeFeatureList = new List<IObject>();
                                                                                                    }
                                                                                                    ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                    found = true;

                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if (Globals.IsNumeric(test))
                                                                                                    {
                                                                                                        if (sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeSmallInteger ||
                                                                                                            sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeInteger)
                                                                                                        {

                                                                                                            sourceFeatureLocal.set_Value(sourceField, Convert.ToInt32(test));


                                                                                                            if (ChangeFeatureList == null)
                                                                                                            {
                                                                                                                ChangeFeatureList = new List<IObject>();
                                                                                                            }
                                                                                                            ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                            found = true;

                                                                                                        }
                                                                                                        else if (sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeDouble ||
                                                                                                            sourceFeatureLocal.Fields.get_Field(sourceField).Type == esriFieldType.esriFieldTypeSingle)
                                                                                                        {
                                                                                                            sourceFeatureLocal.set_Value(sourceField, Convert.ToDouble(test));

                                                                                                            if (ChangeFeatureList == null)
                                                                                                            {
                                                                                                                ChangeFeatureList = new List<IObject>();
                                                                                                            }
                                                                                                            ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                            found = true;

                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            sourceFeatureLocal.set_Value(sourceField, test as object);


                                                                                                            if (ChangeFeatureList == null)
                                                                                                            {
                                                                                                                ChangeFeatureList = new List<IObject>();
                                                                                                            }
                                                                                                            ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                            found = true;

                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        sourceFeatureLocal.set_Value(sourceField, test as object);


                                                                                                        if (ChangeFeatureList == null)
                                                                                                        {
                                                                                                            ChangeFeatureList = new List<IObject>();
                                                                                                        }
                                                                                                        ChangeFeatureList.Add(sourceFeatureLocal);
                                                                                                        found = true;

                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                        catch
                                                                                        {
                                                                                            AAState.WriteLine("                  ERROR setting value");
                                                                                        }
                                                                                        finally
                                                                                        {

                                                                                        }


                                                                                    }

                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR/WARNING: Source Layer is not editable: " + sourceLayerName);
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                    }
                                                                }
                                                            }
                                                            if (found)
                                                            {
                                                                break;

                                                            }

                                                        }

                                                    }
                                                    fLocalCursor = null;
                                                    sourceFeatureLocal = null;
                                                }
                                                catch (Exception ex)
                                                {
                                                    AAState.WriteLine("                  ERROR: UPDATE_INTERSECTING_FEATURE" + Environment.NewLine + ex.Message);
                                                }

                                                finally
                                                {
                                                    AAState.WriteLine("                  Finished: UPDATE_INTERSECTING_FEATURE");

                                                }
                                                break;

                                            }
                                        case "MULTI_FIELD_INTERSECT":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: MULTI_FIELD_INTERSECT");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    int popFldIdx = 0;
                                                    if (args.GetLength(0) > 2)
                                                    {
                                                        AAState.WriteLine("                  Parsing Valueinfo");

                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString().Trim();
                                                        string[] fieldsToPop = args[2].ToString().Split(',');
                                                        if (args.GetLength(0) == 4)
                                                        {
                                                            AAState.WriteLine("                  Search distance specified");

                                                            if (Globals.IsDouble(args[3]))
                                                            {
                                                                searchDistance = Convert.ToDouble(args[3]);
                                                            }
                                                            else
                                                            {
                                                                searchDistance = 0.0;
                                                            }
                                                        }
                                                        else
                                                        {

                                                            searchDistance = 0.0;
                                                        }

                                                        if (sourceFieldName != null)
                                                        {
                                                            AAState.WriteLine("                  Looping Through Layers");

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                if (fieldsToPop.Length == popFldIdx)
                                                                    break;


                                                                sourceLayerName = sourceLayerNames[i].ToString().Trim();
                                                                if (sourceLayerName != "")
                                                                {

                                                                    boolLayerOrFC = true;
                                                                    if (sourceLayerName.Contains("("))
                                                                    {
                                                                        string[] tempSplt = sourceLayerName.Split('(');
                                                                        sourceLayerName = tempSplt[0].Trim();
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                    }

                                                                    else
                                                                    {
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                    }
                                                                    if (sourceLayer != null)
                                                                    {
                                                                        if (sourceLayer.FeatureClass != null)
                                                                        {
                                                                            sourceField = Globals.GetFieldIndex(sourceLayer.FeatureClass.Fields, sourceFieldName);

                                                                            if (sourceField > -1)
                                                                            {

                                                                                sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, false);


                                                                                pFS = (IFeatureSelection)sourceLayer;
                                                                                if (boolLayerOrFC)
                                                                                {
                                                                                    if (pFS.SelectionSet.Count > 0)
                                                                                    {
                                                                                        pFS.SelectionSet.Search(sFilter, true, out cCurs);
                                                                                        fCursor = cCurs as IFeatureCursor;


                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        fCursor = sourceLayer.Search(sFilter, true);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                                }

                                                                                while ((sourceFeature = fCursor.NextFeature()) != null)
                                                                                {
                                                                                    if (sourceFeature.Class != inFeature.Class)
                                                                                    {
                                                                                        if (fieldsToPop.Length == popFldIdx)
                                                                                            break;

                                                                                        string test = sourceFeature.get_Value(sourceField).ToString().Trim();

                                                                                        int tempFieldNum = Globals.GetFieldIndex(inObject.Fields, fieldsToPop[popFldIdx]);
                                                                                        popFldIdx++;
                                                                                        if (tempFieldNum > -1)
                                                                                        {
                                                                                            inObject.set_Value(tempFieldNum, sourceFeature.get_Value(sourceField));
                                                                                        }

                                                                                    }
                                                                                    else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                                                    {
                                                                                        if (fieldsToPop.Length == popFldIdx)
                                                                                            break;

                                                                                        string test = sourceFeature.get_Value(sourceField).ToString().Trim();

                                                                                        int tempFieldNum = Globals.GetFieldIndex(inObject.Fields, fieldsToPop[popFldIdx]);
                                                                                        popFldIdx++;
                                                                                        if (tempFieldNum > -1)
                                                                                        {
                                                                                            inObject.set_Value(tempFieldNum, sourceFeature.get_Value(sourceField));
                                                                                        }

                                                                                    }
                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR/WARNING: Datasource is invalid: " + sourceLayerName);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR/WARNING: Source Layer string is empty");

                                                                }
                                                            }


                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Field name is invalid");


                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Invalid Value method definition");

                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: MULTI_FIELD_INTERSECT: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: MULTI_FIELD_INTERSECT");
                                            }
                                            break;
                                        case "INTERSECT_STATS":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECT_STATS");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    //LayerToIntersect|Field To Elevate
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    int AverageCount = 0;
                                                    if (args.GetLength(0) > 2)
                                                    {
                                                        AAState.WriteLine("                  Parsing Valueinfo");

                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString().Trim();
                                                        string statType = args[2].ToString().Trim();
                                                        if (args.GetLength(0) == 4)
                                                        {
                                                            AAState.WriteLine("                  Search distance specified");

                                                            if (Globals.IsDouble(args[3]))
                                                                searchDistance = Convert.ToDouble(args[3]);
                                                            else
                                                                searchDistance = 0.0;
                                                        }
                                                        else
                                                        {

                                                            searchDistance = 0.0;
                                                        }
                                                        double result = -999999.1;
                                                        string textRes = "";
                                                        if (sourceFieldName != null)
                                                        {
                                                            AAState.WriteLine("                  Looping Through Layers");

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {

                                                                sourceLayerName = sourceLayerNames[i].ToString().Trim();
                                                                if (sourceLayerName != "")
                                                                {
                                                                    boolLayerOrFC = true;
                                                                    if (sourceLayerName.Contains("("))
                                                                    {
                                                                        string[] tempSplt = sourceLayerName.Split('(');
                                                                        sourceLayerName = tempSplt[0].Trim();
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                    }

                                                                    else
                                                                    {
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);
                                                                    }

                                                                    if (sourceLayer != null)
                                                                    {
                                                                        if (sourceLayer.FeatureClass != null)
                                                                        {
                                                                            sourceField = Globals.GetFieldIndex(sourceLayer.FeatureClass.Fields, sourceFieldName);

                                                                            if (sourceField > -1)
                                                                            {

                                                                                sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, false);

                                                                                pFS = (IFeatureSelection)sourceLayer;
                                                                                if (boolLayerOrFC)
                                                                                {
                                                                                    if (pFS.SelectionSet.Count > 0)
                                                                                    {
                                                                                        pFS.SelectionSet.Search(sFilter, true, out cCurs);
                                                                                        fCursor = cCurs as IFeatureCursor;


                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        fCursor = sourceLayer.Search(sFilter, true);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                                }
                                                                                sourceFeature = fCursor.NextFeature();
                                                                                while (sourceFeature != null)
                                                                                {
                                                                                    if (sourceFeature.Class != inFeature.Class)
                                                                                    {
                                                                                        string test = sourceFeature.get_Value(sourceField).ToString().Trim();
                                                                                        if (Globals.IsNumeric(test))
                                                                                        {

                                                                                            double valToTest = Convert.ToDouble(test);
                                                                                            if (result == -999999.1)
                                                                                            {
                                                                                                result = valToTest;


                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                switch (statType.ToUpper())
                                                                                                {
                                                                                                    case "MAX":
                                                                                                        if (result < valToTest)
                                                                                                        {
                                                                                                            result = valToTest;

                                                                                                        }
                                                                                                        break;
                                                                                                    case "MIN":
                                                                                                        if (result > valToTest)
                                                                                                        {
                                                                                                            result = valToTest;

                                                                                                        }
                                                                                                        break;
                                                                                                    case "SUM":
                                                                                                        result += valToTest;


                                                                                                        break;
                                                                                                    case "AVERAGE":
                                                                                                        result += valToTest;
                                                                                                        AverageCount++;
                                                                                                        break;
                                                                                                    case "MEAN":
                                                                                                        result += valToTest;
                                                                                                        AverageCount++;

                                                                                                        break;
                                                                                                    case "CONCAT":
                                                                                                        if (textRes.Contains(valToTest.ToString() + ConcatDelim))
                                                                                                        {
                                                                                                        }
                                                                                                        else if (textRes.Contains(ConcatDelim + valToTest.ToString()))
                                                                                                        {
                                                                                                        }
                                                                                                        else
                                                                                                        {

                                                                                                            if (textRes == "")
                                                                                                            {
                                                                                                                textRes += valToTest.ToString();
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                textRes += ConcatDelim + valToTest.ToString();
                                                                                                            }

                                                                                                        }

                                                                                                        break;
                                                                                                    default:
                                                                                                        AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                                        break;
                                                                                                }

                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            switch (statType.ToUpper())
                                                                                            {

                                                                                                case "CONCAT":
                                                                                                    if (textRes.Contains(test.ToString() + ConcatDelim))
                                                                                                    {
                                                                                                    }
                                                                                                    else if (textRes.Contains(ConcatDelim + test.ToString()))
                                                                                                    {
                                                                                                    }
                                                                                                    else
                                                                                                    {

                                                                                                        if (textRes == "")
                                                                                                        {
                                                                                                            textRes += test;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            textRes += ConcatDelim + test;
                                                                                                        }
                                                                                                    }


                                                                                                    break;
                                                                                                default:
                                                                                                    AAState.WriteLine("                  ERROR/WARNING: Non numeric value returned: " + test);

                                                                                                    AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                                    break;
                                                                                            }

                                                                                        }

                                                                                    }
                                                                                    else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                                                    {
                                                                                        string test = sourceFeature.get_Value(sourceField).ToString();
                                                                                        if (Globals.IsNumeric(test))
                                                                                        {

                                                                                            double valToTest = Convert.ToDouble(test);
                                                                                            if (result == -999999.1)
                                                                                            {
                                                                                                result = valToTest;


                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                switch (statType.ToUpper())
                                                                                                {
                                                                                                    case "MAX":
                                                                                                        if (result < valToTest)
                                                                                                        {
                                                                                                            result = valToTest;

                                                                                                        }
                                                                                                        break;
                                                                                                    case "MIN":
                                                                                                        if (result > valToTest)
                                                                                                        {
                                                                                                            result = valToTest;

                                                                                                        }
                                                                                                        break;
                                                                                                    case "SUM":
                                                                                                        result += valToTest;


                                                                                                        break;
                                                                                                    case "AVERAGE":
                                                                                                        result += valToTest;
                                                                                                        AverageCount++;
                                                                                                        break;
                                                                                                    case "MEAN":
                                                                                                        result += valToTest;
                                                                                                        AverageCount++;

                                                                                                        break;
                                                                                                    case "CONCAT":
                                                                                                        if (textRes.Contains(valToTest.ToString() + ConcatDelim))
                                                                                                        {
                                                                                                        }
                                                                                                        else if (textRes.Contains(ConcatDelim + valToTest.ToString()))
                                                                                                        {
                                                                                                        }
                                                                                                        else
                                                                                                        {

                                                                                                            if (textRes == "")
                                                                                                            {
                                                                                                                textRes += valToTest.ToString();
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                textRes += ConcatDelim + valToTest.ToString();
                                                                                                            }


                                                                                                        }
                                                                                                        break;
                                                                                                    default:
                                                                                                        AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                                        break;
                                                                                                }

                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            switch (statType.ToUpper())
                                                                                            {

                                                                                                case "CONCAT":
                                                                                                    if (textRes.Contains(test.ToString() + ConcatDelim))
                                                                                                    {
                                                                                                    }
                                                                                                    else if (textRes.Contains(ConcatDelim + test.ToString()))
                                                                                                    {
                                                                                                    }
                                                                                                    else
                                                                                                    {

                                                                                                        if (textRes == "")
                                                                                                        {
                                                                                                            textRes += test;
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            textRes += ConcatDelim + test;
                                                                                                        }


                                                                                                    }
                                                                                                    break;
                                                                                                default:
                                                                                                    AAState.WriteLine("                  ERROR/WARNING: Non numeric value returned: " + test);

                                                                                                    AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                                    break;
                                                                                            }

                                                                                        }
                                                                                    }
                                                                                    sourceFeature = fCursor.NextFeature();

                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR/WARNING: Datasource is invalid: " + sourceLayerName);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR/WARNING: Source Layer string is empty");

                                                                }
                                                            }
                                                            try
                                                            {
                                                                if (textRes != "")
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], textRes);
                                                                }
                                                                else if (result != -999999.1)
                                                                {
                                                                    if (AverageCount != 0)
                                                                    {
                                                                        result = result / AverageCount;
                                                                    }
                                                                    inObject.set_Value(intFldIdxs[0], result);

                                                                }
                                                                else
                                                                {
                                                                    IField field = inObject.Fields.get_Field(intFldIdxs[0]);
                                                                    object newval = field.DefaultValue;
                                                                    if (newval == null)
                                                                    {
                                                                        if (field.IsNullable)
                                                                        {
                                                                            inObject.set_Value(intFldIdxs[0], null);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], newval);
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                AAState.WriteLine("                  ERROR: Value was not set on feature: " + ex.Message);

                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Field name is invalid");


                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Invalid Value method definition");

                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECT_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECT_STATS");
                                            }
                                            break;
                                        case "FEATURE_STATS":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: FEATURE_STATS");
                                                if (inFeature != null & valData != null)
                                                {

                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    //LayerToIntersect|Field To Elevate
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    int AverageCount = 0;
                                                    if (args.GetLength(0) > 1)
                                                    {
                                                        AAState.WriteLine("                  Parsing Valueinfo");


                                                        string[] sourceFieldNames = args[0].ToString().Split(',');
                                                        string statType = args[1].ToString();

                                                        double result = -999999.1;
                                                        string textRes = "";
                                                        if (sourceFieldNames != null)
                                                        {
                                                            AAState.WriteLine("                  Looping Through Fields");

                                                            for (int i = 0; i < sourceFieldNames.Length; i++)
                                                            {

                                                                sourceFieldName = sourceFieldNames[i].ToString();
                                                                if (sourceFieldName != "")
                                                                {

                                                                    sourceField = Globals.GetFieldIndex(inObject.Fields, sourceFieldName);

                                                                    if (sourceField > -1)
                                                                    {
                                                                        string test = inObject.get_Value(sourceField).ToString();
                                                                        if (Globals.IsNumeric(test))
                                                                        {

                                                                            double valToTest = Convert.ToDouble(test);
                                                                            if (result == -999999.1)
                                                                            {
                                                                                result = valToTest;


                                                                            }
                                                                            else
                                                                            {
                                                                                switch (statType.ToUpper())
                                                                                {
                                                                                    case "MAX":
                                                                                        if (result < valToTest)
                                                                                        {
                                                                                            result = valToTest;

                                                                                        }
                                                                                        break;
                                                                                    case "MIN":
                                                                                        if (result > valToTest)
                                                                                        {
                                                                                            result = valToTest;

                                                                                        }
                                                                                        break;
                                                                                    case "SUM":
                                                                                        result += valToTest;


                                                                                        break;
                                                                                    case "AVERAGE":
                                                                                        result += valToTest;
                                                                                        AverageCount++;
                                                                                        break;
                                                                                    case "MEAN":
                                                                                        result += valToTest;
                                                                                        AverageCount++;

                                                                                        break;
                                                                                    case "CONCAT":
                                                                                        if (textRes.Contains(valToTest.ToString() + ConcatDelim))
                                                                                        {
                                                                                        }
                                                                                        else if (textRes.Contains(ConcatDelim + valToTest.ToString()))
                                                                                        {
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if (textRes == "")
                                                                                            {
                                                                                                textRes += valToTest.ToString();
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                textRes += ConcatDelim + valToTest.ToString();
                                                                                            }
                                                                                        }


                                                                                        break;
                                                                                    default:
                                                                                        AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                        break;
                                                                                }

                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            switch (statType.ToUpper())
                                                                            {

                                                                                case "CONCAT":
                                                                                    if (textRes.Contains(test.ToString() + ConcatDelim))
                                                                                    {
                                                                                    }
                                                                                    else if (textRes.Contains(ConcatDelim + test.ToString()))
                                                                                    {
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        if (textRes == "")
                                                                                        {
                                                                                            textRes += test;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            textRes += ConcatDelim + test;
                                                                                        }
                                                                                    }


                                                                                    break;
                                                                                default:
                                                                                    AAState.WriteLine("                  ERROR/WARNING: Non numeric value returned: " + test);

                                                                                    AAState.WriteLine("                  ERROR: Unsupported stat type: " + test);
                                                                                    break;
                                                                            }
                                                                        }
                                                                    }
                                                                }


                                                            }
                                                            try
                                                            {
                                                                if (textRes != "")
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], textRes);
                                                                }
                                                                else if (result != -999999.1)
                                                                {
                                                                    if (AverageCount != 0)
                                                                    {
                                                                        result = result / AverageCount;
                                                                    }
                                                                    inObject.set_Value(intFldIdxs[0], result);

                                                                }
                                                                else
                                                                {
                                                                    IField field = inObject.Fields.get_Field(intFldIdxs[0]);
                                                                    object newval = field.DefaultValue;
                                                                    if (newval == null)
                                                                    {
                                                                        if (field.IsNullable)
                                                                        {
                                                                            inObject.set_Value(intFldIdxs[0], null);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        inObject.set_Value(intFldIdxs[0], newval);
                                                                    }
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                AAState.WriteLine("                  ERROR: Value was not set on feature: " + ex.Message);

                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: Field name is invalid");


                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Invalid Value method definition");

                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: FEATURE_STATS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: FEATURE_STATS");
                                            }
                                            break;
                                        case "INTERSECTING_EDGE":
                                            try
                                            {
                                                if (valData == null) break;
                                                args = valData.Split('|');
                                                switch (args.GetLength(0))
                                                {
                                                    case 1:
                                                        sourceFieldName = args[0].ToString();
                                                        break;
                                                    case 2:
                                                        sourceFieldName = args[1].ToString();
                                                        break;

                                                    default: break;
                                                }


                                                AAState.WriteLine("                  Trying: INTERSECTING_EDGE");

                                                if (inFeature != null)
                                                {
                                                    netFeat = inFeature as INetworkFeature;
                                                    AAState.WriteLine("                  Checking if it is a network feature");
                                                    if (netFeat != null)
                                                    {
                                                        AAState.WriteLine("                  Feature is a network feature");
                                                        if (inFeature.FeatureType == esriFeatureType.esriFTComplexJunction || inFeature.FeatureType == esriFeatureType.esriFTSimpleJunction)
                                                        {
                                                            AAState.WriteLine("                  Feature is a junction feature");
                                                            iJuncFeat = (IJunctionFeature)netFeat;

                                                            ISimpleJunctionFeature iSJunc = iJuncFeat as ISimpleJunctionFeature;
                                                            if (iSJunc == null)
                                                                break;
                                                            AAState.WriteLine("                  Edge Count: " + iSJunc.EdgeFeatureCount);
                                                            if (iSJunc.EdgeFeatureCount <= 0)
                                                                break;
                                                            if (iSJunc.EdgeFeatureCount > 0)
                                                            {
                                                                for (int i = 0; i < iSJunc.EdgeFeatureCount; i++)
                                                                {

                                                                    iEdgeFeat = iSJunc.get_EdgeFeature(i);
                                                                    try
                                                                    {


                                                                        IRow pRow = iEdgeFeat as IRow;

                                                                        // verify that field (in junction) to copy exists
                                                                        AAState.WriteLine("                  Getting Field From Edge");
                                                                        juncField = Globals.GetFieldIndex(pRow.Fields, sourceFieldName);
                                                                        string test = pRow.get_Value(juncField).ToString();
                                                                        AAState.WriteLine("                  Attempting to store: " + test);
                                                                        inObject.set_Value(intFldIdxs[0], test);
                                                                        continue;
                                                                    }
                                                                    catch
                                                                    {
                                                                    }
                                                                }//end loop
                                                                try
                                                                {


                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    AAState.WriteLine("                  ERROR: Value was not set on feature: " + ex.Message);

                                                                }

                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  No Connected Edges Found");
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  not an junction feature");
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  Not a Geometric Network Feature");
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_EDGE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_EDGE");
                                            }
                                            break;
                                        case "INTERSECTING_FEATURE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_FEATURE");
                                                if (inFeature != null & valData != null)
                                                {
                                                    AAState.WriteLine("                  Starting to process rule");
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;
                                                    found = false;
                                                    AAState.WriteLine("                  Value Info is: " + valData);
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    //if (args.GetLength(0) >= 2)
                                                    if (args.Length >= 2)
                                                    {
                                                        AAState.intersectOptions strOpt = AAState.intersectOptions.First;
                                                        switch (args.Length)
                                                        {
                                                            case 2:
                                                                sourceLayerNames = args[0].ToString().Split(',');
                                                                sourceFieldName = args[1].ToString();
                                                                break;
                                                            case 3:
                                                                sourceLayerNames = args[0].ToString().Split(',');
                                                                sourceFieldName = args[1].ToString();
                                                                switch (args[2].ToString().ToUpper())
                                                                {
                                                                    case "PROMPT":
                                                                        strOpt = AAState.intersectOptions.PromptMulti;
                                                                        break;
                                                                    case "P":
                                                                        strOpt = AAState.intersectOptions.PromptMulti;
                                                                        break;
                                                                    case "CENTROID":
                                                                        strOpt = AAState.intersectOptions.Centroid;
                                                                        break;
                                                                    case "C":
                                                                        strOpt = AAState.intersectOptions.Centroid;
                                                                        break;
                                                                    case "F":
                                                                        strOpt = AAState.intersectOptions.First;
                                                                        break;
                                                                    case "FIRST":
                                                                        strOpt = AAState.intersectOptions.First;
                                                                        break;
                                                                }
                                                                break;
                                                            default: break;
                                                        }




                                                        if (sourceFieldName != null)
                                                        {
                                                            List<Globals.OptionsToPresent> pFoundFeat = new List<Globals.OptionsToPresent>();

                                                            for (int i = 0; i < sourceLayerNames.Length; i++)
                                                            {
                                                                sourceLayerName = sourceLayerNames[i].ToString().Trim();
                                                                if (sourceLayerName != "")
                                                                {
                                                                    boolLayerOrFC = true;


                                                                    if (sourceLayerName.Contains("("))
                                                                    {
                                                                        string[] tempSplt = sourceLayerName.Split('(');
                                                                        sourceLayerName = tempSplt[0].Trim();
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                                        if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }


                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                        {
                                                                            boolLayerOrFC = true;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }
                                                                        else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                        {
                                                                            boolLayerOrFC = false;
                                                                        }

                                                                    }
                                                                    else
                                                                    {
                                                                        sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                                    }
                                                                    // Get layer

                                                                    if (sourceLayer != null)
                                                                    {
                                                                        AAState.WriteLine("                  Layer Found: " + sourceLayer.Name);

                                                                        sourceField = Globals.GetFieldIndex(sourceLayer.FeatureClass.Fields, sourceFieldName);
                                                                        AAState.WriteLine("                  " + sourceFieldName + ": at " + sourceField);
                                                                        if (sourceField > -1)
                                                                        {

                                                                            sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, strOpt == AAState.intersectOptions.Centroid);
                                                                            if (sFilter == null)
                                                                            {
                                                                                AAState.WriteLine("                  Filter is null, this will cause an error");
                                                                                continue;
                                                                            }


                                                                            pFS = (IFeatureSelection)sourceLayer;
                                                                            if (boolLayerOrFC)
                                                                            {
                                                                                AAState.WriteLine("                  Searching on Layer");
                                                                                if (pFS.SelectionSet.Count > 0)
                                                                                {
                                                                                    AAState.WriteLine("                  Searching on Selection Set");
                                                                                    pFS.SelectionSet.Search(sFilter, true, out cCurs);

                                                                                    fCursor = cCurs as IFeatureCursor;

                                                                                    AAState.WriteLine("                  Cursor created");
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  No Selected Features");
                                                                                    fCursor = sourceLayer.Search(sFilter, true);
                                                                                    AAState.WriteLine("                  Cursor created");
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Searching on feature Class");
                                                                                fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                                AAState.WriteLine("                  Cursor created");
                                                                            }
                                                                            if (fCursor == null)
                                                                            {
                                                                                AAState.WriteLine("                  Cursor is null");
                                                                                continue;
                                                                            }
                                                                            AAState.WriteLine("                  Starring Loop of found features");
                                                                            while ((sourceFeature = fCursor.NextFeature()) != null)
                                                                            {
                                                                                AAState.WriteLine("                  Checking Class");
                                                                                if (sourceFeature.Class != inFeature.Class)
                                                                                {
                                                                                    AAState.WriteLine("                  Different FCs");
                                                                                    if (strOpt == AAState.intersectOptions.PromptMulti)
                                                                                    {

                                                                                        Globals.OptionsToPresent pOp = new Globals.OptionsToPresent();
                                                                                        pOp.Display = sourceFeature.get_Value(Globals.GetFieldIndex(sourceFeature.Fields, sourceLayer.DisplayField)).ToString();
                                                                                        if (pOp.Display.Trim() != "")
                                                                                        {
                                                                                            pOp.Display = sourceLayer.Name + ": " + pOp.Display;

                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            pOp.Display = sourceLayer.Name + ": " + sourceFeature.OID;

                                                                                        }
                                                                                        pOp.OID = sourceFeature.OID;
                                                                                        pOp.LayerName = sourceLayer.Name;

                                                                                        pFoundFeat.Add(pOp);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        string test = sourceFeature.get_Value(sourceField).ToString();
                                                                                        AAState.WriteLine("                  Setting Value: " + test);
                                                                                        inObject.set_Value(intFldIdxs[0], sourceFeature.get_Value(sourceField));
                                                                                        AAState.WriteLine("                  Value Set");
                                                                                        found = true;
                                                                                    }
                                                                                }
                                                                                else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                                                {
                                                                                    AAState.WriteLine("                  Same FC");
                                                                                    if (strOpt == AAState.intersectOptions.PromptMulti)
                                                                                    {
                                                                                        Globals.OptionsToPresent pOp = new Globals.OptionsToPresent();
                                                                                        pOp.Display = sourceFeature.get_Value(Globals.GetFieldIndex(sourceFeature.Fields, sourceLayer.DisplayField)).ToString();
                                                                                        if (pOp.Display.Trim() != "")
                                                                                        {
                                                                                            pOp.Display = sourceLayer.Name + ": " + pOp.Display;

                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            pOp.Display = sourceLayer.Name + ": " + sourceFeature.OID;

                                                                                        }
                                                                                        pOp.OID = sourceFeature.OID;
                                                                                        pOp.LayerName = sourceLayer.Name;

                                                                                        pFoundFeat.Add(pOp);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        string test = sourceFeature.get_Value(sourceField).ToString();
                                                                                        AAState.WriteLine("                  Setting Value: " + test);

                                                                                        inObject.set_Value(intFldIdxs[0], sourceFeature.get_Value(sourceField));
                                                                                        AAState.WriteLine("                  Value Set");

                                                                                        found = true;
                                                                                    }
                                                                                }
                                                                                if (found == true)
                                                                                    break;

                                                                            }




                                                                            if (found == false && AAState._CheckEnvelope && pFoundFeat.Count == 0)
                                                                            {
                                                                                sFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;


                                                                                pFS = (IFeatureSelection)sourceLayer;
                                                                                if (boolLayerOrFC)
                                                                                {
                                                                                    if (pFS.SelectionSet.Count > 0)
                                                                                    {
                                                                                        pFS.SelectionSet.Search(sFilter, true, out cCurs);
                                                                                        fCursor = cCurs as IFeatureCursor;


                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        fCursor = sourceLayer.Search(sFilter, true);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                                }
                                                                                sourceFeature = fCursor.NextFeature();
                                                                                while (sourceFeature != null)
                                                                                {
                                                                                    if (strOpt == AAState.intersectOptions.PromptMulti)
                                                                                    {
                                                                                        Globals.OptionsToPresent pOp = new Globals.OptionsToPresent();
                                                                                        pOp.Display = sourceFeature.get_Value(Globals.GetFieldIndex(sourceFeature.Fields, sourceLayer.DisplayField)).ToString();
                                                                                        if (pOp.Display.Trim() != "")
                                                                                        {
                                                                                            pOp.Display = sourceLayer.Name + ": " + pOp.Display;

                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            pOp.Display = sourceLayer.Name + ": " + sourceFeature.OID;

                                                                                        }
                                                                                        pOp.OID = sourceFeature.OID;
                                                                                        pOp.LayerName = sourceLayer.Name;

                                                                                        pFoundFeat.Add(pOp);

                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        if (found)
                                                                                        {
                                                                                            break;
                                                                                        }
                                                                                        string test = sourceFeature.get_Value(sourceField).ToString();
                                                                                        AAState.WriteLine("                  Setting Value: " + test);

                                                                                        inObject.set_Value(intFldIdxs[0], sourceFeature.get_Value(sourceField));
                                                                                        AAState.WriteLine("                  Value Set");

                                                                                        found = true;

                                                                                    }
                                                                                    sourceFeature = fCursor.NextFeature();
                                                                                }


                                                                            }


                                                                            if (found)
                                                                            {
                                                                                break;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  ERROR: Source field not found: " + sourceFieldName);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR/WARNING: Source Layer not found: " + sourceLayerName);
                                                                    }
                                                                }

                                                            }
                                                            if (pFoundFeat.Count > 0 && strOpt == AAState.intersectOptions.PromptMulti)
                                                            {
                                                                Globals.OptionsToPresent strRetVal = Globals.showOptionsForm(pFoundFeat, "Select A feature for " + sourceFieldName, "Select A feature for " + sourceFieldName, ComboBoxStyle.DropDownList);
                                                                if (strRetVal != null)
                                                                {
                                                                    sourceFeature = sourceLayer.FeatureClass.GetFeature(strRetVal.OID);

                                                                    string test = sourceFeature.get_Value(sourceField).ToString();
                                                                    AAState.WriteLine("                  Setting Value: " + test);

                                                                    inObject.set_Value(intFldIdxs[0], sourceFeature.get_Value(sourceField));
                                                                    AAState.WriteLine("                  Value Set");

                                                                    found = true;

                                                                }
                                                            }

                                                        }
                                                        else
                                                        {
                                                            AAState.WriteLine("                  ERROR: INTERSECTING_FEATURE: source field name is null");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: INTERSECTING_FEATURE: Value Info is not in the expected format");
                                                    }
                                                }
                                                else
                                                {
                                                    AAState.WriteLine("                  ERROR: INTERSECTING_FEATURE: Input Feature or Value Info is null");
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_FEATURE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_FEATURE");
                                            }
                                            break;
                                        case "INTERSECTING_RASTER":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_RASTER");
                                                if (inFeature != null & valData != null)
                                                {

                                                    sourceLayerName = "";
                                                    formatString = "";
                                                    found = false;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.Length < 1) break;
                                                    switch (args.Length)
                                                    {
                                                        case 1:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            break;
                                                        case 2:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            formatString = args[1].ToString();
                                                            break;
                                                        default: break;
                                                    }

                                                    // Get layer
                                                    for (int i = 0; i < sourceLayerNames.Length; i++)
                                                    {
                                                        sourceLayerName = sourceLayerNames[i].ToString().Trim();
                                                        IPoint pLoc = Globals.GetGeomCenter(inFeature)[0];

                                                        if (pLoc != null)
                                                        {
                                                            string cellVal = Globals.GetCellValue(sourceLayerName, pLoc, AAState._editor.Map);
                                                            AAState.WriteLine("                  ERROR/WARING: No cell value or raster was found: " + sourceLayerName);
                                                            if (cellVal != null && cellVal != "" && cellVal != "No Raster")
                                                            {

                                                                if (formatString == null || formatString == "" || (inObject.Fields.get_Field(intFldIdxs[0]).Type != esriFieldType.esriFieldTypeString))
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], cellVal);
                                                                    found = true;
                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    formatString = formatString + cellVal;
                                                                    inObject.set_Value(intFldIdxs[0], formatString);

                                                                    found = true;
                                                                    break;
                                                                }

                                                            }

                                                        }
                                                    }
                                                    if (!(found) && inObject.Fields.get_Field(intFldIdxs[0]).IsNullable)
                                                        inObject.set_Value(intFldIdxs[0], null);
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_RASTER: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_RASTER");
                                            }
                                            break;
                                        case "INTERSECTING_LAYER_DETAILS":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_LAYER_DETAILS");
                                                if (inFeature != null & valData != null)
                                                {

                                                    sourceLayerName = "";
                                                    formatString = "";
                                                    found = false;
                                                    List<string> matchPattern = null;
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.Length >= 1)
                                                    {
                                                        AAState.intersectOptions strOpt = AAState.intersectOptions.First;

                                                        switch (args.Length)
                                                        {
                                                            case 1:
                                                                sourceLayerNames = args[0].ToString().Split(',');
                                                                formatString = "P";
                                                                break;
                                                            case 2:
                                                                sourceLayerNames = args[0].ToString().Split(',');
                                                                formatString = args[1].ToString();
                                                                break;
                                                            case 3:
                                                                sourceLayerNames = args[0].ToString().Split(',');
                                                                formatString = args[1].ToString();
                                                                switch (args[2].ToString().ToUpper())
                                                                {
                                                                    case "PROMPT":
                                                                        strOpt = AAState.intersectOptions.PromptMulti;
                                                                        break;
                                                                    case "P":
                                                                        strOpt = AAState.intersectOptions.PromptMulti;
                                                                        break;
                                                                    case "CENTROID":
                                                                        strOpt = AAState.intersectOptions.Centroid;
                                                                        break;
                                                                    case "C":
                                                                        strOpt = AAState.intersectOptions.Centroid;
                                                                        break;
                                                                    case "F":
                                                                        strOpt = AAState.intersectOptions.First;
                                                                        break;
                                                                    case "FIRST":
                                                                        strOpt = AAState.intersectOptions.First;
                                                                        break;
                                                                }
                                                                break;
                                                            case 4:
                                                                sourceLayerNames = args[0].ToString().Split(',');
                                                                formatString = args[1].ToString();
                                                                switch (args[2].ToString().ToUpper())
                                                                {
                                                                    case "PROMPT":
                                                                        strOpt = AAState.intersectOptions.PromptMulti;
                                                                        break;
                                                                    case "P":
                                                                        strOpt = AAState.intersectOptions.PromptMulti;
                                                                        break;
                                                                    case "CENTROID":
                                                                        strOpt = AAState.intersectOptions.Centroid;
                                                                        break;
                                                                    case "C":
                                                                        strOpt = AAState.intersectOptions.Centroid;
                                                                        break;
                                                                    case "F":
                                                                        strOpt = AAState.intersectOptions.First;
                                                                        break;
                                                                    case "FIRST":
                                                                        strOpt = AAState.intersectOptions.First;
                                                                        break;
                                                                }
                                                                matchPattern = new List<string>(args[3].ToString().Split(','));

                                                                break;
                                                            default: break;

                                                        }
                                                        List<Globals.OptionsToPresent> strFiles = new List<Globals.OptionsToPresent>();
                                                        // Get layer
                                                        for (int i = 0; i < sourceLayerNames.Length; i++)
                                                        {
                                                            sourceLayerName = sourceLayerNames[i].ToString().Trim();

                                                            IGeometry pGeo = inFeature.ShapeCopy;
                                                            List<IGeometry> pGeos = new List<IGeometry>();

                                                            if (pGeo != null)
                                                            {
                                                                if (strOpt == AAState.intersectOptions.Centroid)
                                                                {
                                                                    List<IPoint> pGeoPnts = Globals.GetGeomCenter(pGeo);
                                                                    pGeos = pGeoPnts.ConvertAll(new Converter<IPoint, IGeometry>(Globals.PointToGeometry));

                                                                }
                                                                else
                                                                {
                                                                    pGeos.Add(pGeo);
                                                                }
                                                                AAState.WriteLine("                  Getting list of " + sourceLayerName + " Layers");
                                                                IEnumLayer pEnum = Globals.GetLayers(AAState._editor.Map, sourceLayerName);

                                                                if (pEnum != null)
                                                                {
                                                                    AAState.WriteLine("                  List retrieved of " + sourceLayerName + " Layers");
                                                                    AAState.WriteLine("                  Starting Loop");
                                                                    ILayer pLay = pEnum.Next();

                                                                    while (pLay != null)
                                                                    {


                                                                        intersectLayerDetailsFunctions(pLay, pGeos, strOpt, ref found, ref strFiles, ref inObject, intFldIdxs[0], matchPattern);


                                                                        if (found)
                                                                        {
                                                                            AAState.WriteLine("                  Exiting Layer Loop");
                                                                            break;
                                                                        }


                                                                        pLay = pEnum.Next();
                                                                    }
                                                                    pLay = null;
                                                                    pEnum = null;
                                                                }
                                                                else
                                                                {
                                                                    bool FCorLayerTemp = true;
                                                                    ILayer pLay = Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref FCorLayerTemp);
                                                                    if (pLay != null)
                                                                    {


                                                                        intersectLayerDetailsFunctions(pLay, pGeos, strOpt, ref found, ref strFiles, ref inObject, intFldIdxs[0], matchPattern);

                                                                    }
                                                                    pLay = null;
                                                                }

                                                                if (pEnum != null)
                                                                    Marshal.ReleaseComObject(pEnum);
                                                                pEnum = null;
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: Geo not Found");
                                                            }
                                                            if (found)
                                                            {
                                                                AAState.WriteLine("                  Exiting Layer Loop");
                                                                break;
                                                            }
                                                            pGeo = null;
                                                            pGeos = null;
                                                        }
                                                        if (strOpt == AAState.intersectOptions.PromptMulti && strFiles.Count > 0)
                                                        {
                                                            Globals.OptionsToPresent strRetVal = Globals.showOptionsForm(strFiles, "Select a intersect layer for " + strFldNames[0], "Select a intersect layer" + strFldNames[0], ComboBoxStyle.DropDownList);
                                                            if (strRetVal != null)
                                                            {
                                                                try
                                                                {
                                                                    inObject.set_Value(intFldIdxs[0], strRetVal.Display);
                                                                }
                                                                catch
                                                                {
                                                                    MessageBox.Show("Error Setting the value in the Intersecting_Layer_Details");

                                                                }
                                                                found = true;
                                                            }
                                                        }

                                                    }
                                                    else
                                                    {
                                                        AAState.WriteLine("                  ERROR: Value info is not correct");
                                                    }

                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_LAYER_DETAILS: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_LAYER_DETAILS");
                                            }
                                            break;
                                        case "INTERSECTING_FEATURE_DISTANCE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: INTERSECTING_FEATURE_DISTANCE");
                                                if (inFeature != null & valData != null)
                                                {
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    sourceField = -1;

                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    if (args.GetLength(0) >= 2)
                                                    {

                                                        sourceLayerNames = args[0].ToString().Split(',');
                                                        sourceFieldName = args[1].ToString().Trim();
                                                    }
                                                    // Get layer

                                                    if (sourceFieldName != null)
                                                    {
                                                        for (int i = 0; i < sourceLayerNames.Length; i++)
                                                        {
                                                            sourceLayerName = sourceLayerNames[i].ToString();
                                                            if (sourceLayerName != "")

                                                                sourceLayerName = args[i].ToString();
                                                            if (i == 0)
                                                                i++;
                                                            boolLayerOrFC = true;

                                                            if (sourceLayerName.Contains("("))
                                                            {
                                                                string[] tempSplt = sourceLayerName.Split('(');
                                                                sourceLayerName = tempSplt[0];
                                                                sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                                if (tempSplt[1].ToUpper().Contains("LAYER)"))
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURELAYER)"))
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURECLASS)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("CLASS)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else if (tempSplt[1].ToUpper().Contains("FEATURE)"))
                                                                {
                                                                    boolLayerOrFC = false;
                                                                }
                                                                else
                                                                {
                                                                    boolLayerOrFC = true;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                sourceLayer = (IFeatureLayer)Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref boolLayerOrFC);

                                                            }
                                                            if (sourceLayer == null)
                                                            {
                                                                AAState.WriteLine("                  ERROR/WARNING: " + sourceLayerName + " was not found");
                                                                continue;
                                                            }

                                                            IFeatureClass iFC = inFeature.Class as IFeatureClass;
                                                            if (sourceLayer.FeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                                                            {
                                                                AAState.WriteLine("                  ERROR: " + sourceLayer + " is a polygon layer");

                                                                break;
                                                            }
                                                            if (sourceLayer != null)
                                                            {
                                                                sourceField = Globals.GetFieldIndex(sourceLayer.FeatureClass.Fields, sourceFieldName);

                                                                if (sourceField > -1)
                                                                {
                                                                    sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, false);

                                                                    pFS = (IFeatureSelection)sourceLayer;
                                                                    if (boolLayerOrFC)
                                                                    {
                                                                        if (pFS.SelectionSet.Count > 0)
                                                                        {
                                                                            pFS.SelectionSet.Search(sFilter, true, out cCurs);
                                                                            fCursor = cCurs as IFeatureCursor;


                                                                        }
                                                                        else
                                                                        {
                                                                            fCursor = sourceLayer.Search(sFilter, true);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        fCursor = sourceLayer.FeatureClass.Search(sFilter, true);
                                                                    }

                                                                    while ((sourceFeature = fCursor.NextFeature()) != null)
                                                                    {
                                                                        if (sourceFeature.Class != inFeature.Class)
                                                                        {
                                                                            IPoint pIntPnt;
                                                                            if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                                            {
                                                                                pIntPnt = Globals.GetIntersection(inFeature.ShapeCopy, sourceFeature.ShapeCopy as IPolyline) as IPoint;

                                                                            }
                                                                            else
                                                                                pIntPnt = inFeature.ShapeCopy as IPoint;
                                                                            IPoint snapPnt = null;

                                                                            double dAlong = Globals.PointDistanceOnLine(pIntPnt, sourceFeature.Shape as IPolyline, 2, out snapPnt);
                                                                            snapPnt = null;

                                                                            string strUnit = Globals.GetSpatRefUnitName(Globals.GetLayersCoordinateSystem(sourceLayer.FeatureClass), true);
                                                                            if (strUnit == "Foot" && dAlong != 1)
                                                                            {
                                                                                strUnit = "Feet";
                                                                            }
                                                                            else if (strUnit == "Meter" && dAlong != 1)
                                                                            {
                                                                                strUnit = "Meters";
                                                                            }
                                                                            string strDis = dAlong + " " + strUnit + " along " + sourceLayer.Name + " with " + sourceLayer.FeatureClass.Fields.get_Field(sourceField).AliasName + " of " + sourceFeature.get_Value(sourceField);



                                                                            if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                            {

                                                                                strDis = dAlong + " " + strUnit + ": " + sourceFeature.get_Value(sourceField);
                                                                                AAState.WriteLine("                  Text is to long, defaulting to length along: " + strDis);

                                                                                if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                                {

                                                                                    if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                                    {
                                                                                        strDis = dAlong.ToString();
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    strDis = dAlong.ToString();
                                                                                    AAState.WriteLine("                  Text is to long, defaulting to length along: " + strDis);
                                                                                    if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                                    {
                                                                                        strDis = dAlong.ToString();
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Value set to: " + strDis);
                                                                                inObject.set_Value(intFldIdxs[0], strDis);
                                                                                break;
                                                                            }
                                                                        }

                                                                        else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                                        {
                                                                            IPoint pIntPnt;
                                                                            if (inFeature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                                                                            {
                                                                                pIntPnt = Globals.GetIntersection(inFeature.ShapeCopy, sourceFeature.ShapeCopy as IPolyline) as IPoint;

                                                                            }
                                                                            else
                                                                                pIntPnt = inFeature.ShapeCopy as IPoint;
                                                                            IPoint snapPnt = null;


                                                                            double dAlong = Globals.PointDistanceOnLine(pIntPnt, sourceFeature.Shape as IPolyline, 2, out snapPnt);
                                                                            snapPnt = null;

                                                                            string strUnit = Globals.GetSpatRefUnitName(Globals.GetLayersCoordinateSystem(sourceLayer.FeatureClass), true);
                                                                            if (strUnit == "Foot" && dAlong != 1)
                                                                            {
                                                                                strUnit = "Feet";
                                                                            }
                                                                            else if (strUnit == "Meter" && dAlong != 1)
                                                                            {
                                                                                strUnit = "Meters";
                                                                            }
                                                                            string strDis = dAlong + " " + strUnit + " along " + sourceLayer.Name + " with " + sourceLayer.FeatureClass.Fields.get_Field(sourceField).AliasName + " of " + sourceFeature.get_Value(sourceField);



                                                                            if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                            {

                                                                                strDis = dAlong + " " + strUnit + ": " + sourceFeature.get_Value(sourceField);
                                                                                AAState.WriteLine("                  Text is to long, defaulting to length along: " + strDis);

                                                                                if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                                {

                                                                                    if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                                    {
                                                                                        strDis = dAlong.ToString();
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    strDis = dAlong.ToString();
                                                                                    AAState.WriteLine("                  Text is to long, defaulting to length along: " + strDis);
                                                                                    if (inObject.Fields.get_Field(intFldIdxs[0]).Length < strDis.Length - 1)
                                                                                    {
                                                                                        strDis = dAlong.ToString();
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        inObject.set_Value(intFldIdxs[0], strDis);
                                                                                        break;
                                                                                    }
                                                                                }

                                                                            }
                                                                            else
                                                                            {
                                                                                AAState.WriteLine("                  Value set to: " + strDis);
                                                                                inObject.set_Value(intFldIdxs[0], strDis);
                                                                                break;
                                                                            }
                                                                        }
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayer + ": field: " + sourceFieldName + " was not found");
                                                                }
                                                            }
                                                            else { }
                                                        }
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: INTERSECTING_FEATURE_DISTANCE: " + ex.Message);
                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: INTERSECTING_FEATURE_DISTANCE");
                                            }
                                            break;


                                        //Release: 1.2
                                        //New Dynamic Value Method: NEARSET_FEATURE - similiar to INTERSECTING_FEATURE but requires a search distance.  

                                        case "NEAREST_FEATURE":
                                            try
                                            {
                                                AAState.WriteLine("                  Trying: NEAREST_FEATURE");
                                                if (inFeature != null & valData != null)
                                                {
                                                    string sourceMatField = "";
                                                    string targetMatField = "";
                                                    sourceLayerName = "";
                                                    sourceFieldName = "";
                                                    searchDistance = 0;
                                                    found = false;
                                                    // Parse arguments
                                                    args = valData.Split('|');
                                                    switch (args.Length)
                                                    {
                                                        case 2:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            sourceFieldName = args[1].ToString();
                                                            break;
                                                        case 3:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            sourceFieldName = args[1].ToString();

                                                            Double.TryParse(args[2], out searchDistance);
                                                            break;
                                                        case 4:
                                                            AAState.WriteLine("                  ERROR: Incorrect value method");
                                                            break;

                                                        case 5:
                                                            sourceLayerNames = args[0].ToString().Split(',');
                                                            sourceFieldName = args[1].ToString();
                                                            Double.TryParse(args[2], out searchDistance);
                                                            sourceMatField = args[3].ToString();
                                                            targetMatField = args[4].ToString();
                                                            break;
                                                        default:
                                                            AAState.WriteLine("                  ERROR: Incorrect value method");
                                                            break;

                                                    }

                                                    if (sourceLayerNames.Length > 0 & sourceFieldName != null)
                                                    {
                                                        for (int i = 0; i < sourceLayerNames.Length; i++)
                                                        {
                                                            sourceLayerName = sourceLayerNames[i].ToString();
                                                            if (sourceLayerName != "")
                                                            {
                                                                bool FCorLayerSource = true;
                                                                sourceLayer = Globals.FindLayer(AAState._editor.Map, sourceLayerName, ref FCorLayerSource) as IFeatureLayer;
                                                                if (sourceLayer != null)
                                                                {
                                                                    sourceField = Globals.GetFieldIndex(sourceLayer.FeatureClass.Fields, sourceFieldName);

                                                                    if (sourceField > -1)
                                                                    {

                                                                        sFilter = Globals.createSpatialFilter(sourceLayer, inFeature, searchDistance, false);
                                                                        pFS = (IFeatureSelection)sourceLayer;
                                                                        if (boolLayerOrFC)
                                                                        {
                                                                            AAState.WriteLine("                  Searching the layer");
                                                                            if (pFS.SelectionSet.Count > 0)
                                                                            {
                                                                                AAState.WriteLine("                  There is a seleciton set, only checking selected features");
                                                                                pFS.SelectionSet.Search(sFilter, false, out cCurs);
                                                                                fCursor = cCurs as IFeatureCursor;


                                                                            }
                                                                            else
                                                                            {

                                                                                fCursor = sourceLayer.Search(sFilter, false);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            AAState.WriteLine("                  Searching the feature class");
                                                                            fCursor = sourceLayer.FeatureClass.Search(sFilter, false);
                                                                        }


                                                                        nearestFeature = null;

                                                                        proxOp = (IProximityOperator)inFeature.ShapeCopy;
                                                                        lastDistance = searchDistance;
                                                                        while ((sourceFeature = fCursor.NextFeature()) != null)
                                                                        {
                                                                            if (sourceFeature.Class != inFeature.Class)
                                                                            {
                                                                                AAState.WriteLine("                  Feature Classes are different");
                                                                                if (targetMatField == "" && sourceMatField == "")
                                                                                {

                                                                                    AAState.WriteLine("                  No matching fields specified");
                                                                                    try
                                                                                    {
                                                                                        IGeometry pTempGeo = sourceFeature.ShapeCopy;
                                                                                        pTempGeo.Project(inFeature.Shape.SpatialReference);

                                                                                        distance = proxOp.ReturnDistance(pTempGeo);
                                                                                        pTempGeo = null;

                                                                                        if (distance <= lastDistance)
                                                                                        {
                                                                                            nearestFeature = sourceFeature;
                                                                                            lastDistance = distance;
                                                                                        }
                                                                                        pTempGeo = null;
                                                                                    }
                                                                                    catch
                                                                                    {
                                                                                        MessageBox.Show("Error in the Nearest Feature - Proximity Operator.  This may be caused by corrupt data in the layer, Please run Check/Repair Geometry GP tool on your layer");
                                                                                        return false;


                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  matching fields specified");
                                                                                    int idxTargetFld = Globals.GetFieldIndex(sourceLayer, targetMatField);
                                                                                    int idxSourceFld = Globals.GetFieldIndex(inObject.Fields, sourceMatField);
                                                                                    if (idxSourceFld >= 0 && idxTargetFld >= 0)
                                                                                    {
                                                                                        AAState.WriteLine("                  Fields Found");
                                                                                        if (inObject.get_Value(idxSourceFld).ToString() == sourceFeature.get_Value(idxTargetFld).ToString())
                                                                                        {
                                                                                            AAState.WriteLine("                  Values Match");
                                                                                            IGeometry pTempGeo = sourceFeature.ShapeCopy;
                                                                                            pTempGeo.Project(inFeature.Shape.SpatialReference);

                                                                                            distance = proxOp.ReturnDistance(pTempGeo);
                                                                                            pTempGeo = null;

                                                                                            if (distance <= lastDistance)
                                                                                            {
                                                                                                nearestFeature = sourceFeature;
                                                                                                lastDistance = distance;
                                                                                            }
                                                                                            pTempGeo = null;

                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            AAState.WriteLine("                  Values does not Match: " + inObject.get_Value(idxSourceFld).ToString() + " - " + sourceFeature.get_Value(idxTargetFld).ToString());

                                                                                        }

                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        AAState.WriteLine("                  Fields Not Found");
                                                                                        IGeometry pTempGeo = sourceFeature.ShapeCopy;
                                                                                        pTempGeo.Project(inFeature.Shape.SpatialReference);

                                                                                        distance = proxOp.ReturnDistance(pTempGeo);
                                                                                        pTempGeo = null;

                                                                                        if (distance <= lastDistance)
                                                                                        {
                                                                                            nearestFeature = sourceFeature;
                                                                                            lastDistance = distance;
                                                                                        }
                                                                                        pTempGeo = null;

                                                                                    }

                                                                                }
                                                                            }
                                                                            else if (sourceFeature.Class == inFeature.Class && sourceFeature.OID != inFeature.OID)
                                                                            {
                                                                                AAState.WriteLine("                  matching fields specified");
                                                                                int idxTargetFld = Globals.GetFieldIndex(sourceLayer, targetMatField);
                                                                                int idxSourceFld = Globals.GetFieldIndex(inObject.Fields, sourceMatField);
                                                                                if (idxSourceFld >= 0 && idxTargetFld >= 0)
                                                                                {
                                                                                    AAState.WriteLine("                  Fields Found");
                                                                                    if (inObject.get_Value(idxSourceFld).ToString() == sourceFeature.get_Value(idxTargetFld).ToString())
                                                                                    {
                                                                                        AAState.WriteLine("                  Values Match");
                                                                                        IGeometry pTempGeo = sourceFeature.ShapeCopy;
                                                                                        pTempGeo.Project(inFeature.Shape.SpatialReference);

                                                                                        distance = proxOp.ReturnDistance(pTempGeo);
                                                                                        pTempGeo = null;

                                                                                        if (distance <= lastDistance)
                                                                                        {
                                                                                            nearestFeature = sourceFeature;
                                                                                            lastDistance = distance;
                                                                                        }
                                                                                        pTempGeo = null;

                                                                                    }

                                                                                }
                                                                                else
                                                                                {
                                                                                    AAState.WriteLine("                  Fields Not Found");
                                                                                    IGeometry pTempGeo = sourceFeature.ShapeCopy;
                                                                                    pTempGeo.Project(inFeature.Shape.SpatialReference);

                                                                                    distance = proxOp.ReturnDistance(pTempGeo);
                                                                                    pTempGeo = null;

                                                                                    if (distance <= lastDistance)
                                                                                    {
                                                                                        nearestFeature = sourceFeature;
                                                                                        lastDistance = distance;
                                                                                    }
                                                                                    pTempGeo = null;
                                                                                }

                                                                            }

                                                                        }

                                                                        if (nearestFeature != null)
                                                                        {
                                                                            AAState.WriteLine("                  Feature found: " + nearestFeature.Class.AliasName + ":" + nearestFeature.OID);
                                                                            inObject.set_Value(intFldIdxs[0], nearestFeature.get_Value(sourceField));
                                                                            found = true;
                                                                            break;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        AAState.WriteLine("                  ERROR: " + sourceLayer + ": field: " + sourceFieldName + " was not found");
                                                                    }

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                  ERROR: " + sourceLayer + " was not found");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                AAState.WriteLine("                  ERROR: Empty source layer name");
                                                            }

                                                        }
                                                        if (!found)
                                                        {

                                                        }
                                                    }
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                AAState.WriteLine("                  ERROR: NEAREST_FEATURE: " + ex.Message);

                                            }
                                            finally
                                            {
                                                AAState.WriteLine("                  Finished: NEAREST_FEATURE");
                                            }
                                            break;

                                        default:
                                            AAState.WriteLine("ERROR: " + valMethod + " for layer " + tableName + " is not a valid method, check the dynamic value table");

                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("TableName:" + tableName + "  FieldName:" + strFldNames[0] + System.Environment.NewLine + "ValueMethod:" + valMethod + "  ValueData:" + valData + System.Environment.NewLine + "Message: " + ex.Message, "Attribute Assistant Message");
                                    AAState.WriteLine("ERROR: TableName:" + tableName + "  FieldName:" + strFldNames[0] + System.Environment.NewLine + "ValueMethod:" + valMethod + "  ValueData:" + valData + System.Environment.NewLine + "Message: " + ex.Message);
                                }
                            }
                            else
                            {
                                AAState.WriteLine("      Rule not processed");

                            }


                            try
                            {
                                if (intFldIdxs.Count > 0 && strFldNames.Count > 0)
                                {
                                    for (int p = 0; p < strFldNames.Count; p++)
                                    {

                                        IRowChanges inChanges = inObject as IRowChanges;
                                        bool changed = inChanges.get_ValueChanged(intFldIdxs[p]);
                                        if (changed)
                                            try
                                            {
                                                if (AAState.lastValueProperties.GetProperty(strFldNames[p]) != null)
                                                {
                                                    LastValueEntry lstVal = AAState.lastValueProperties.GetProperty(strFldNames[p]) as LastValueEntry;
                                                    if (lstVal != null)
                                                    {
                                                        if (mode == "ON_CREATE" && lstVal.On_Create == false)
                                                        {
                                                            string test = "";
                                                        }
                                                        else if (mode == "ON_MANUAL" && lstVal.On_Manual == false)
                                                        {
                                                            string test = "";
                                                        }
                                                        else if (mode == "ON_CHANGE" && lstVal.On_ChangeAtt == false)
                                                        {
                                                            string test = "";
                                                        }
                                                        else if (mode == "ON_CHANGEGEO" && lstVal.On_ChangeGeo == false)
                                                        {
                                                            string test = "";
                                                        }
                                                        else
                                                        {
                                                            if (lstVal.Value != null)
                                                            {
                                                                AAState.WriteLine("                      Setting Last Value");
                                                                if (mode == "ON_CREATE" && (inObject.get_Value(intFldIdxs[p]) == null || inObject.get_Value(intFldIdxs[p]).ToString() == ""))
                                                                {
                                                                    AAState.WriteLine("                           Null Value in the create Method, not storing in last value array");

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                           " + strFldNames[p] + ": " + inObject.get_Value(intFldIdxs[p]).ToString());
                                                                    lstVal.Value = inObject.get_Value(intFldIdxs[p]);

                                                                    AAState.lastValueProperties.SetProperty(strFldNames[p], lstVal);
                                                                }
                                                            }

                                                            else
                                                            {
                                                                if (mode == "ON_CREATE" && (inObject.get_Value(intFldIdxs[p]) == null || inObject.get_Value(intFldIdxs[p]).ToString() == ""))
                                                                {
                                                                    AAState.WriteLine("                           Null Value in the create Method, not storing in last value array");

                                                                }
                                                                else
                                                                {
                                                                    AAState.WriteLine("                      Setting Last Value");
                                                                    AAState.WriteLine("                           " + strFldNames[p] + ": " + inObject.get_Value(intFldIdxs[p]).ToString());
                                                                    lstVal.Value = inObject.get_Value(intFldIdxs[p]);

                                                                    AAState.lastValueProperties.SetProperty(strFldNames[p], lstVal);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                            catch
                                            {


                                            }

                                    }
                                }
                            }
                            catch
                            {
                                AAState.WriteLine("        Error Setting Last Value");

                            }

                            AAState.WriteLine("    ------------------------------------------------");

                        }

                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing the rules" + System.Environment.NewLine + "Message:" + ex.Message, "Attribute Assistant Message");
                AAState.WriteLine("Error processing the rules");
                return false;

            }
            finally
            {
                if (AAState._tab == inObject.Class || AAState._gentab == inObject.Class)
                {
                    AAState.reInitExt();

                }
                if (progressDialog != null)
                {
                    progressDialog.HideDialog();
                }
                AAState.WriteLine("DONE");
                AAState.WriteLine("---------------------------------------");
                if (fCursor != null)
                {
                    Marshal.ReleaseComObject(fCursor);
                    GC.Collect(300);
                    GC.WaitForFullGCComplete();
                }
                inFeature = null;

                mseg = null;
                netFeat = null;
                iEdgeFeat = null;

                iJuncFeat = null;

                progressDialogFactory = null;
                stepProgressor = null;
                progressDialog = null;
                trackCancel = null;

            }

        }

        //Returns rows from defaults table which match this object's table name or wildcard
        private void getDefaultRows(ref IObject inObject, out ICursor outCursor)
        {
            outCursor = null;
            try
            {


                AAState._gentab = Globals.FindTable(ArcMap.Document.FocusMap, AAState._sequenceTableName);

                ITable tab = Globals.FindTable(ArcMap.Document.FocusMap, AAState._defaultsTableName);
                if (tab != null)
                {
                    IDataset dataset = inObject.Class as IDataset;
                    IQueryFilter qFilter = new QueryFilterClass();
                    IQueryFilterDefinition qFilterDef = qFilter as IQueryFilterDefinition;
                    qFilterDef.PostfixClause = "ORDERBY TABLENAME";
                    string[] items = dataset.Name.Split('.');
                    string name = items[items.GetLength(0) - 1];
                    qFilter.WhereClause = "TABLENAME = '" + name + "' or TABLENAME = '*'";
                    outCursor = tab.Search(qFilter, true) as ICursor;
                }
                else
                {
                    outCursor = null;
                }
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getDefaultRows: " + ex.Message);

            }
        }

        private void getView(DataTable dt)
        {
            try
            {
                DataView view = new DataView(dt);
                foreach (DataRowView drv in view)
                {
                    Console.WriteLine(drv["OBJECTID"].ToString());
                }
                view.RowFilter = "ValData = 'Evy'";
                foreach (DataRowView drv in view)
                {
                    Console.WriteLine(drv["OBJECTID"].ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetView: " + ex.Message);
            }
        }


        private void intersectLayerDetailsFunctions(ILayer pLay, List<IGeometry> pGeos, AAState.intersectOptions strOpt, ref bool found,
                                               ref List<Globals.OptionsToPresent> strFiles, ref ESRI.ArcGIS.Geodatabase.IObject inObject, int intFldIdx, List<string> MatchPattern)
        {

            AAState.WriteLine("                  Checking " + pLay.Name);


            IRasterLayer pRasLay = null;

            IEnvelope pEnv = null;

            IRelationalOperator pRel = null;
            IRelationalOperator2 pRel2 = null;
            ISpatialFilter pSpatFilt = null;
            IFeatureLayer pFLay = null;
            try
            {

                if (pLay is IRasterLayer)
                {

                    pRasLay = pLay as IRasterLayer;

                    AAState.WriteLine("                  Trying " + pRasLay.Name);
                    pEnv = pRasLay.AreaOfInterest;

                    pRel = pEnv as IRelationalOperator;
                    pRel2 = pEnv as IRelationalOperator2;
                    foreach (IGeometry pGeo in pGeos)
                    {
                        if (pRel.Crosses(pGeo) || pRel.Touches(pGeo) || pRel.Overlaps(pGeo) || pRel2.ContainsEx(pGeo, esriSpatialRelationExEnum.esriSpatialRelationExClementini))
                        {
                            AAState.WriteLine("                  Geometry does intersect " + pRasLay.Name);
                            string pathForLay = Globals.GetPathForALayer(pLay);

                            switch (formatString)
                            {
                                case "P":

                                    if (MatchPattern == null)
                                    {
                                        if (strOpt == AAState.intersectOptions.PromptMulti)
                                        {
                                            if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                            {
                                                strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                            }
                                        }
                                        else
                                        {
                                            inObject.set_Value(intFldIdx, pathForLay);
                                            pRasLay = null;
                                            pEnv = null;

                                            pRel = null;
                                            pRel2 = null;
                                            found = true;
                                            return;
                                        }
                                    }
                                    else if (MatchPattern.Count == 0)
                                    {
                                        if (strOpt == AAState.intersectOptions.PromptMulti)
                                        {
                                            if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                            {
                                                strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                            }
                                        }
                                        else
                                        {
                                            inObject.set_Value(intFldIdx, pathForLay);
                                            pRasLay = null;
                                            pEnv = null;

                                            pRel = null;
                                            pRel2 = null;
                                            found = true;
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        foreach (string MatPat in MatchPattern)
                                        {
                                            if (pathForLay.ToUpper().Contains(MatPat.ToUpper()) || pLay.Name.ToUpper().Contains(MatPat.ToUpper()))
                                            {

                                                if (strOpt == AAState.intersectOptions.PromptMulti)
                                                {
                                                    if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                    {
                                                        strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                    }
                                                }
                                                else
                                                {
                                                    inObject.set_Value(intFldIdx, pathForLay);
                                                    pRasLay = null;
                                                    pEnv = null;

                                                    pRel = null;
                                                    pRel2 = null;
                                                    found = true;
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case "N":
                                    if (MatchPattern == null)
                                    {
                                        if (strOpt == AAState.intersectOptions.PromptMulti)
                                        {
                                            if (!strFiles.Contains(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name)))
                                            {
                                                strFiles.Add(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name));
                                            }
                                        }
                                        else
                                        {
                                            inObject.set_Value(intFldIdx, pLay.Name);
                                            pRasLay = null;
                                            pEnv = null;

                                            pRel = null;
                                            pRel2 = null;
                                            found = true;
                                            return;
                                        }
                                    }
                                    else if (MatchPattern.Count == 0)
                                    {
                                        if (strOpt == AAState.intersectOptions.PromptMulti)
                                        {
                                            if (!strFiles.Contains(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name)))
                                            {
                                                strFiles.Add(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name));
                                            }
                                        }
                                        else
                                        {
                                            inObject.set_Value(intFldIdx, pLay.Name);
                                            pRasLay = null;
                                            pEnv = null;

                                            pRel = null;
                                            pRel2 = null;
                                            found = true;
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        foreach (string MatPat in MatchPattern)
                                        {
                                            if (pathForLay.ToUpper().Contains(MatPat.ToUpper()) || pLay.Name.ToUpper().Contains(MatPat.ToUpper()))
                                            {

                                                if (strOpt == AAState.intersectOptions.PromptMulti)
                                                {
                                                    if (!strFiles.Contains(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name)))
                                                    {
                                                        strFiles.Add(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name));
                                                    }
                                                }
                                                else
                                                {
                                                    inObject.set_Value(intFldIdx, pLay.Name);
                                                    pRasLay = null;
                                                    pEnv = null;

                                                    pRel = null;
                                                    pRel2 = null;
                                                    found = true;
                                                    return;
                                                }
                                            }
                                        }
                                    }

                                    break;
                                default:

                                    if (MatchPattern == null)
                                    {
                                        if (strOpt == AAState.intersectOptions.PromptMulti)
                                        {
                                            if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                            {
                                                strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                            }
                                        }
                                        else
                                        {
                                            inObject.set_Value(intFldIdx, pathForLay);
                                            pRasLay = null;
                                            pEnv = null;

                                            pRel = null;
                                            pRel2 = null;
                                            found = true;
                                            return;
                                        }
                                    }
                                    else if (MatchPattern.Count == 0)
                                    {
                                        if (strOpt == AAState.intersectOptions.PromptMulti)
                                        {
                                            if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                            {
                                                strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                            }
                                        }
                                        else
                                        {
                                            inObject.set_Value(intFldIdx, pathForLay);
                                            pRasLay = null;
                                            pEnv = null;

                                            pRel = null;
                                            pRel2 = null;
                                            found = true;
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        foreach (string MatPat in MatchPattern)
                                        {
                                            if (pathForLay.ToUpper().Contains(MatPat.ToUpper()) || pLay.Name.ToUpper().Contains(MatPat.ToUpper()))
                                            {

                                                if (strOpt == AAState.intersectOptions.PromptMulti)
                                                {
                                                    if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                    {
                                                        strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                    }
                                                }
                                                else
                                                {
                                                    inObject.set_Value(intFldIdx, pathForLay);
                                                    pRasLay = null;
                                                    pEnv = null;

                                                    pRel = null;
                                                    pRel2 = null;
                                                    found = true;
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }

                }
                else if (pLay is IFeatureLayer)
                {
                    pFLay = pLay as IFeatureLayer;
                    if (pFLay.FeatureClass == inObject.Class)
                    {
                    }
                    else
                    {
                        AAState.WriteLine("                  Trying " + pFLay.Name);
                        foreach (IGeometry pGeo in pGeos)
                        {

                            pSpatFilt = new SpatialFilterClass();
                            pSpatFilt.GeometryField = pFLay.FeatureClass.ShapeFieldName;
                            pSpatFilt.Geometry = pGeo as IGeometry;
                            pSpatFilt.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                            if (pFLay.FeatureClass.FeatureCount(pSpatFilt) > 0)
                            {
                                AAState.WriteLine("                  Geometry does intersect " + pFLay.Name);
                                string pathForLay = Globals.GetPathForALayer(pLay);

                                switch (formatString)
                                {
                                    case "P":

                                        if (MatchPattern == null)
                                        {
                                            if (strOpt == AAState.intersectOptions.PromptMulti)
                                            {
                                                if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                {
                                                    strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                }
                                            }
                                            else
                                            {
                                                inObject.set_Value(intFldIdx, pathForLay);
                                                pRasLay = null;
                                                pEnv = null;

                                                pRel = null;
                                                pRel2 = null;
                                                found = true;
                                                return;
                                            }
                                        }
                                        else if (MatchPattern.Count == 0)
                                        {
                                            if (strOpt == AAState.intersectOptions.PromptMulti)
                                            {
                                                if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                {
                                                    strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                }
                                            }
                                            else
                                            {
                                                inObject.set_Value(intFldIdx, pathForLay);
                                                pRasLay = null;
                                                pEnv = null;

                                                pRel = null;
                                                pRel2 = null;
                                                found = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            foreach (string MatPat in MatchPattern)
                                            {
                                                if (pathForLay.ToUpper().Contains(MatPat.ToUpper()) || pLay.Name.ToUpper().Contains(MatPat.ToUpper()))
                                                {

                                                    if (strOpt == AAState.intersectOptions.PromptMulti)
                                                    {
                                                        if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                        {
                                                            strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        inObject.set_Value(intFldIdx, pathForLay);
                                                        pRasLay = null;
                                                        pEnv = null;

                                                        pRel = null;
                                                        pRel2 = null;
                                                        found = true;
                                                        return;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case "N":
                                        if (MatchPattern == null)
                                        {
                                            if (strOpt == AAState.intersectOptions.PromptMulti)
                                            {
                                                if (!strFiles.Contains(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name)))
                                                {
                                                    strFiles.Add(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name));
                                                }
                                            }
                                            else
                                            {
                                                inObject.set_Value(intFldIdx, pLay.Name);
                                                pRasLay = null;
                                                pEnv = null;

                                                pRel = null;
                                                pRel2 = null;
                                                found = true;
                                                return;
                                            }
                                        }
                                        else if (MatchPattern.Count == 0)
                                        {
                                            if (strOpt == AAState.intersectOptions.PromptMulti)
                                            {
                                                if (!strFiles.Contains(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name)))
                                                {
                                                    strFiles.Add(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name));
                                                }
                                            }
                                            else
                                            {
                                                inObject.set_Value(intFldIdx, pLay.Name);
                                                pRasLay = null;
                                                pEnv = null;

                                                pRel = null;
                                                pRel2 = null;
                                                found = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            foreach (string MatPat in MatchPattern)
                                            {

                                                if (pathForLay.ToUpper().Contains(MatPat.ToUpper()) || pLay.Name.ToUpper().Contains(MatPat.ToUpper()))
                                                {

                                                    if (strOpt == AAState.intersectOptions.PromptMulti)
                                                    {
                                                        if (!strFiles.Contains(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name)))
                                                        {
                                                            strFiles.Add(new Globals.OptionsToPresent(0, pLay.Name, pLay.Name));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        inObject.set_Value(intFldIdx, pLay.Name);
                                                        pRasLay = null;
                                                        pEnv = null;

                                                        pRel = null;
                                                        pRel2 = null;
                                                        found = true;
                                                        return;
                                                    }
                                                }
                                            }
                                        }

                                        break;
                                    default:

                                        if (MatchPattern == null)
                                        {
                                            if (strOpt == AAState.intersectOptions.PromptMulti)
                                            {
                                                if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                {
                                                    strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                }
                                            }
                                            else
                                            {
                                                inObject.set_Value(intFldIdx, pathForLay);
                                                pRasLay = null;
                                                pEnv = null;

                                                pRel = null;
                                                pRel2 = null;
                                                found = true;
                                                return;
                                            }
                                        }
                                        else if (MatchPattern.Count == 0)
                                        {
                                            if (strOpt == AAState.intersectOptions.PromptMulti)
                                            {
                                                if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                {
                                                    strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                }
                                            }
                                            else
                                            {
                                                inObject.set_Value(intFldIdx, pathForLay);
                                                pRasLay = null;
                                                pEnv = null;

                                                pRel = null;
                                                pRel2 = null;
                                                found = true;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            foreach (string MatPat in MatchPattern)
                                            {
                                                if (pathForLay.ToUpper().Contains(MatPat.ToUpper()) || pLay.Name.ToUpper().Contains(MatPat.ToUpper()))
                                                {

                                                    if (strOpt == AAState.intersectOptions.PromptMulti)
                                                    {
                                                        if (!strFiles.Contains(new Globals.OptionsToPresent(0, pathForLay, pathForLay)))
                                                        {
                                                            strFiles.Add(new Globals.OptionsToPresent(0, pathForLay, pathForLay));
                                                        }
                                                    }
                                                    else
                                                    {
                                                        inObject.set_Value(intFldIdx, pathForLay);
                                                        pRasLay = null;
                                                        pEnv = null;

                                                        pRel = null;
                                                        pRel2 = null;
                                                        found = true;
                                                        return;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                AAState.WriteLine("                  Does not intersect " + pFLay.Name);
                            }
                        }

                    }

                }
                else
                {
                    AAState.WriteLine("                  Warning: Unsupported type");
                }
            }
            catch (Exception ex)
            {
                AAState.WriteLine("                  Error: " + ex.Message);
            }
            finally
            {

                pRasLay = null;

                pEnv = null;

                pRel = null;
                pRel2 = null;
                pSpatFilt = null;
                pFLay = null;
            }
        }
        #endregion
    }
}






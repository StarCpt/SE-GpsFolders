using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VRage.Input;
using VRage.Utils;
using VRage;
using VRageMath;
using Sandbox.Game;

namespace SE_GpsFolders
{
    internal class MyTerminalGpsControllerWrapper
    {
        public static readonly Color ITEM_SHOWN_COLOR = Color.CornflowerBlue;
        private IMyGuiControlsParent m_controlsParent;
        private MyGuiControlSearchBox m_searchBox;
        private StringBuilder m_NameBuilder = new StringBuilder();
        private readonly StringBuilder m_hexSb = new StringBuilder();
        private readonly Regex HEX_REGEX = new Regex("^(#{0,1})([0-9A-Fa-f]{6})$");
        private MyGuiControlTable m_tableIns;
        private MyGuiControlLabel m_labelInsName;
        private MyGuiControlTextbox m_panelInsName;
        private MyGuiControlLabel m_labelInsDesc;
        private MyGuiControlMultilineEditableText m_panelInsDesc;
        private MyGuiControlLabel m_labelInsX;
        private MyGuiControlTextbox m_xCoord;
        private MyGuiControlLabel m_labelInsY;
        private MyGuiControlTextbox m_yCoord;
        private MyGuiControlLabel m_labelInsZ;
        private MyGuiControlTextbox m_zCoord;
        private MyGuiControlLabel m_labelColor;
        private MyGuiControlLabel m_labelHue;
        private MyGuiControlSlider m_sliderHue;
        private MyGuiControlLabel m_labelSaturation;
        private MyGuiControlSlider m_sliderSaturation;
        private MyGuiControlLabel m_labelValue;
        private MyGuiControlSlider m_sliderValue;
        private MyGuiControlLabel m_labelHex;
        private MyGuiControlTextbox m_textBoxHex;
        private MyGuiControlLabel m_labelInsShowOnHud;
        private MyGuiControlCheckbox m_checkInsShowOnHud;
        private MyGuiControlLabel m_labelInsAlwaysVisible;
        private MyGuiControlCheckbox m_checkInsAlwaysVisible;
        private MyGuiControlButton m_buttonAdd;
        private MyGuiControlButton m_buttonAddFromClipboard;
        private MyGuiControlButton m_buttonAddCurrent;
        private MyGuiControlButton m_buttonDelete;
        private MyGuiControlButton m_buttonCopy;
        private MyGuiControlLabel m_labelClipboardGamepadHelp;
        private MyGuiControlLabel m_labelSaveWarning;
        private int? m_previousHash;
        private bool m_needsSyncName;
        private bool m_needsSyncDesc;
        private bool m_needsSyncX;
        private bool m_needsSyncY;
        private bool m_needsSyncZ;
        private bool m_needsSyncColor;
        private string m_clipboardText;
        private bool m_nameOk;
        private bool m_xOk;
        private bool m_yOk;
        private bool m_zOk;
        private MyGps m_syncedGps;

        public void Init(IMyGuiControlsParent controlsParent)
        {
            m_controlsParent = controlsParent;
            m_searchBox = (MyGuiControlSearchBox)m_controlsParent.Controls.GetControlByName("SearchIns");
            m_searchBox.OnTextChanged += SearchIns_TextChanged;
            m_tableIns = (MyGuiControlTable)controlsParent.Controls.GetControlByName("TableINS");
            m_tableIns.SetColumnComparison(0, TableSortingComparison);
            m_tableIns.ItemSelected += OnTableItemSelected;
            m_tableIns.ItemDoubleClicked += OnTableDoubleclick;
            m_buttonAdd = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonAdd");
            m_buttonAddCurrent = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonFromCurrent");
            m_buttonAddFromClipboard = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonFromClipboard");
            m_buttonDelete = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonDelete");
            m_buttonAdd.ButtonClicked += OnButtonPressedNew;
            m_buttonAddFromClipboard.ButtonClicked += OnButtonPressedNewFromClipboard;
            m_buttonAddCurrent.ButtonClicked += OnButtonPressedNewFromCurrent;
            m_buttonDelete.ButtonClicked += OnButtonPressedDelete;
            m_buttonAdd.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_buttonAddCurrent.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_buttonAddFromClipboard.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_buttonDelete.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_labelInsName = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsName");
            m_panelInsName = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("panelInsName");
            m_labelInsDesc = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsDesc");
            m_panelInsDesc = (MyGuiControlMultilineEditableText)controlsParent.Controls.GetControlByName("textInsDesc");
            m_labelInsX = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsX");
            m_xCoord = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("textInsX");
            m_labelInsY = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsY");
            m_yCoord = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("textInsY");
            m_labelInsZ = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsZ");
            m_zCoord = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("textInsZ");
            m_labelInsShowOnHud = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsShowOnHud");
            m_checkInsShowOnHud = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("checkInsShowOnHud");
            m_checkInsShowOnHud.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_ShowOnHud_ToolTip));
            MyGuiControlCheckbox checkInsShowOnHud = m_checkInsShowOnHud;
            checkInsShowOnHud.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Combine(checkInsShowOnHud.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
            m_labelInsAlwaysVisible = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("labelInsAlwaysVisible");
            m_checkInsAlwaysVisible = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("checkInsAlwaysVisible");
            MyGuiControlCheckbox checkInsAlwaysVisible = m_checkInsAlwaysVisible;
            checkInsAlwaysVisible.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Combine(checkInsAlwaysVisible.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnAlwaysVisibleChecked));
            m_buttonCopy = (MyGuiControlButton)m_controlsParent.Controls.GetControlByName("buttonToClipboard");
            m_buttonCopy.ButtonClicked += OnButtonPressedCopy;
            m_labelClipboardGamepadHelp = (MyGuiControlLabel)m_controlsParent.Controls.GetControlByName("labelClipboardGamepadHelp");
            m_labelClipboardGamepadHelp.Visible = false;
            m_labelClipboardGamepadHelp.VisibleChanged += LabelClipboardGamepadHelp_VisibleChanged;
            m_labelSaveWarning = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("TerminalTab_GPS_SaveWarning");
            m_labelSaveWarning.Visible = false;
            m_labelColor = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("gpsColorLabel");
            m_labelHue = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("gpsHueLabel");
            m_sliderHue = (MyGuiControlSlider)controlsParent.Controls.GetControlByName("gpsHueSlider");
            m_labelSaturation = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("gpsSaturationLabel");
            m_sliderSaturation = (MyGuiControlSlider)controlsParent.Controls.GetControlByName("gpsSaturationSlider");
            m_labelValue = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("gpsValueLabel");
            m_sliderValue = (MyGuiControlSlider)controlsParent.Controls.GetControlByName("gpsValueSlider");
            m_labelHex = (MyGuiControlLabel)controlsParent.Controls.GetControlByName("textgpsHexLabelInsZ");
            m_textBoxHex = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("gpsColorHexTextbox");
            m_panelInsName.ShowTooltipWhenDisabled = true;
            m_panelInsDesc.ShowTooltipWhenDisabled = true;
            m_xCoord.ShowTooltipWhenDisabled = true;
            m_yCoord.ShowTooltipWhenDisabled = true;
            m_zCoord.ShowTooltipWhenDisabled = true;
            m_checkInsShowOnHud.ShowTooltipWhenDisabled = true;
            m_checkInsAlwaysVisible.ShowTooltipWhenDisabled = true;
            m_buttonCopy.ShowTooltipWhenDisabled = true;
            HookSyncEvents();
            MySession.Static.Gpss.GpsAdded += OnGpsAdded;
            MySession.Static.Gpss.GpsChanged += OnInsChanged;
            MySession.Static.Gpss.ListChanged += OnListChanged;
            MySession.Static.Gpss.DiscardOld();
            PopulateList();
            m_previousHash = null;
            EnableEditBoxes(enable: false);
            SetDeleteButtonEnabled(enabled: false);
        }

        private int TableSortingComparison(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            if ((((MyGps)a.UserData).DiscardAt.HasValue && ((MyGps)b.UserData).DiscardAt.HasValue) || (!((MyGps)a.UserData).DiscardAt.HasValue && !((MyGps)b.UserData).DiscardAt.HasValue))
            {
                return a.Text.CompareToIgnoreCase(b.Text);
            }
            if (!((MyGps)a.UserData).DiscardAt.HasValue)
            {
                return -1;
            }
            return 1;
        }

        public void PopulateList()
        {
            PopulateList(null);
        }

        public void PopulateList(string searchString)
        {
            object obj = m_tableIns.SelectedRow?.UserData;
            int? num = m_tableIns.SelectedRowIndex;
            ClearList();
            if (MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
            {
                foreach (KeyValuePair<int, MyGps> item in MySession.Static.Gpss[MySession.Static.LocalPlayerId])
                {
                    if (searchString != null)
                    {
                        string[] array = searchString.ToLower().Split(' ');
                        string text = item.Value.Name.ToString().ToLower();
                        bool flag = true;
                        string[] array2 = array;
                        foreach (string text2 in array2)
                        {
                            if (!text.Contains(text2.ToLower()))
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                        {
                            AddToList(item.Value);
                        }
                    }
                    else
                    {
                        AddToList(item.Value);
                    }
                }
            }
            m_tableIns.SortByColumn(0, MyGuiControlTable.SortStateEnum.Ascending);
            EnableEditBoxes(enable: false);
            if (obj != null)
            {
                for (int j = 0; j < m_tableIns.RowsCount; j++)
                {
                    if (obj == m_tableIns.GetRow(j).UserData)
                    {
                        m_tableIns.SelectedRowIndex = j;
                        EnableEditBoxes(enable: true);
                        SetDeleteButtonEnabled(enabled: true);
                        break;
                    }
                }
                if (m_tableIns.SelectedRow == null && obj != null)
                {
                    if (num >= m_tableIns.RowsCount)
                    {
                        num = m_tableIns.RowsCount - 1;
                    }
                    m_tableIns.SelectedRowIndex = num;
                    if (m_tableIns.SelectedRow != null)
                    {
                        EnableEditBoxes(enable: true);
                        SetDeleteButtonEnabled(enabled: true);
                        FillRight((MyGps)m_tableIns.SelectedRow.UserData);
                    }
                }
            }
            m_tableIns.ScrollToSelection();
            if (obj == null)
            {
                FillRight();
            }
        }

        private MyGuiControlTable.Row AddToList(MyGps ins)
        {
            MyGuiControlTable.Row row = new MyGuiControlTable.Row(ins);
            StringBuilder stringBuilder = new StringBuilder(ins.Name);
            string toolTip = stringBuilder.ToString();
            row.AddCell(new MyGuiControlTable.Cell(stringBuilder, ins, toolTip, ins.DiscardAt.HasValue ? Color.Gray : (ins.ShowOnHud ? ins.GPSColor : Color.White)));
            m_tableIns.Add(row);
            return row;
        }

        public void ClearList()
        {
            if (m_tableIns != null)
            {
                m_tableIns.Clear();
            }
        }

        private void SearchIns_TextChanged(string text)
        {
            PopulateList(text);
        }

        private void OnTableItemSelected(MyGuiControlTable sender, MyGuiControlTable.EventArgs args)
        {
            trySync();
            if (sender.SelectedRow != null)
            {
                EnableEditBoxes(enable: true);
                SetDeleteButtonEnabled(enabled: true);
                FillRight((MyGps)sender.SelectedRow.UserData);
                MyGuiControlTable.Cell cell = sender.SelectedRow.GetCell(0);
                if (cell != null)
                {
                    MyGps myGps = (MyGps)m_tableIns.SelectedRow.UserData;
                    cell.TextColor = (myGps.DiscardAt.HasValue ? Color.Gray : (myGps.ShowOnHud ? myGps.GPSColor : Color.White));
                }
            }
            else
            {
                EnableEditBoxes(enable: false);
                SetDeleteButtonEnabled(enabled: false);
                ClearRight();
            }
        }

        private void SetDeleteButtonEnabled(bool enabled)
        {
            if (enabled)
            {
                m_buttonDelete.Enabled = true;
                m_buttonDelete.SetToolTip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_Delete_ToolTip));
            }
            else
            {
                m_buttonDelete.Enabled = false;
                m_buttonDelete.SetToolTip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_Delete_Disabled_ToolTip));
            }
        }

        private void EnableEditBoxes(bool enable)
        {
            m_panelInsName.Enabled = enable;
            m_panelInsDesc.Enabled = enable;
            m_xCoord.Enabled = enable;
            m_yCoord.Enabled = enable;
            m_zCoord.Enabled = enable;
            m_sliderHue.Enabled = enable;
            m_sliderSaturation.Enabled = enable;
            m_sliderValue.Enabled = enable;
            m_textBoxHex.Enabled = enable;
            if (enable)
            {
                m_panelInsName.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewCoord_Name_ToolTip));
                m_panelInsDesc.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_NewCoord_Desc_ToolTip));
                m_xCoord.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_X_ToolTip));
                m_yCoord.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_Y_ToolTip));
                m_zCoord.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_Z_ToolTip));
                m_checkInsShowOnHud.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_ShowOnHud_ToolTip));
                m_checkInsAlwaysVisible.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_AlwaysVisible_Tooltip));
                m_buttonCopy.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_CopyToClipboard_ToolTip));
            }
            else
            {
                m_checkInsShowOnHud.ShowTooltipWhenDisabled = true;
                m_checkInsAlwaysVisible.ShowTooltipWhenDisabled = true;
                m_panelInsName.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
                m_panelInsDesc.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
                m_xCoord.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
                m_yCoord.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
                m_zCoord.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
                m_checkInsShowOnHud.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
                m_checkInsAlwaysVisible.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
                m_buttonCopy.SetTooltip(MyTexts.GetString(MySpaceTexts.TerminalTab_GPS_SelectGpsEntry));
            }
        }

        private void OnTableDoubleclick(MyGuiControlTable sender, MyGuiControlTable.EventArgs args)
        {
            ToggleShowOnHud(sender);
        }

        private static void ToggleShowOnHud(MyGuiControlTable sender)
        {
            if (sender.SelectedRow != null)
            {
                MyGps obj = (MyGps)sender.SelectedRow.UserData;
                obj.ShowOnHud = !obj.ShowOnHud;
                MySession.Static.Gpss.SendChangeShowOnHudRequest(MySession.Static.LocalPlayerId, ((MyGps)sender.SelectedRow.UserData).Hash, ((MyGps)sender.SelectedRow.UserData).ShowOnHud);
            }
        }

        private void HookSyncEvents()
        {
            m_panelInsName.TextChanged += OnNameChanged;
            m_panelInsDesc.TextChanged += OnDescChanged;
            m_xCoord.TextChanged += OnXChanged;
            m_yCoord.TextChanged += OnYChanged;
            m_zCoord.TextChanged += OnZChanged;
            m_textBoxHex.TextChanged += HexTextboxOnTextChanged;
            m_textBoxHex.FocusChanged += HexTextboxOnFocusChanged;
            MyGuiControlSlider sliderHue = m_sliderHue;
            sliderHue.ValueChanged = (Action<MyGuiControlSlider>)Delegate.Combine(sliderHue.ValueChanged, new Action<MyGuiControlSlider>(OnSliderValueChanged));
            MyGuiControlSlider sliderSaturation = m_sliderSaturation;
            sliderSaturation.ValueChanged = (Action<MyGuiControlSlider>)Delegate.Combine(sliderSaturation.ValueChanged, new Action<MyGuiControlSlider>(OnSliderValueChanged));
            MyGuiControlSlider sliderValue = m_sliderValue;
            sliderValue.ValueChanged = (Action<MyGuiControlSlider>)Delegate.Combine(sliderValue.ValueChanged, new Action<MyGuiControlSlider>(OnSliderValueChanged));
        }

        private void UnhookSyncEvents()
        {
            m_panelInsName.TextChanged -= OnNameChanged;
            m_panelInsDesc.TextChanged -= OnDescChanged;
            m_xCoord.TextChanged -= OnXChanged;
            m_yCoord.TextChanged -= OnYChanged;
            m_zCoord.TextChanged -= OnZChanged;
            m_textBoxHex.TextChanged -= HexTextboxOnTextChanged;
            m_textBoxHex.FocusChanged -= HexTextboxOnFocusChanged;
            MyGuiControlSlider sliderHue = m_sliderHue;
            sliderHue.ValueChanged = (Action<MyGuiControlSlider>)Delegate.Remove(sliderHue.ValueChanged, new Action<MyGuiControlSlider>(OnSliderValueChanged));
            MyGuiControlSlider sliderSaturation = m_sliderSaturation;
            sliderSaturation.ValueChanged = (Action<MyGuiControlSlider>)Delegate.Remove(sliderSaturation.ValueChanged, new Action<MyGuiControlSlider>(OnSliderValueChanged));
            MyGuiControlSlider sliderValue = m_sliderValue;
            sliderValue.ValueChanged = (Action<MyGuiControlSlider>)Delegate.Remove(sliderValue.ValueChanged, new Action<MyGuiControlSlider>(OnSliderValueChanged));
        }

        public void OnNameChanged(MyGuiControlTextbox sender)
        {
            if (m_tableIns.SelectedRow == null)
            {
                return;
            }
            m_needsSyncName = true;
            if (IsNameOk(sender.Text))
            {
                m_nameOk = true;
                sender.ColorMask = Vector4.One;
                MyGuiControlTable.Row selectedRow = m_tableIns.SelectedRow;
                MyGuiControlTable.Cell cell = selectedRow.GetCell(0);
                if (cell != null)
                {
                    MyGps myGps = (MyGps)m_tableIns.SelectedRow.UserData;
                    cell.Text.Clear().Append(sender.Text);
                    cell.TextColor = (myGps.DiscardAt.HasValue ? Color.Gray : (myGps.ShowOnHud ? myGps.GPSColor : Color.White));
                    cell.ToolTip.ToolTips[0] = new MyColoredText(sender.Text);
                }
                m_tableIns.SortByColumn(0, MyGuiControlTable.SortStateEnum.Ascending);
                for (int i = 0; i < m_tableIns.RowsCount; i++)
                {
                    if (selectedRow == m_tableIns.GetRow(i))
                    {
                        m_tableIns.SelectedRowIndex = i;
                        break;
                    }
                }
                m_tableIns.ScrollToSelection();
            }
            else
            {
                m_nameOk = false;
                sender.ColorMask = Color.Red.ToVector4();
            }
            UpdateWarningLabel();
        }

        public bool IsNameOk(string str)
        {
            return !str.Contains(":");
        }

        public void OnDescChanged(MyGuiControlMultilineEditableText sender)
        {
            m_needsSyncDesc = true;
        }

        private bool IsCoordOk(string str)
        {
            double result;
            return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public void OnXChanged(MyGuiControlTextbox sender)
        {
            m_needsSyncX = true;
            if (IsCoordOk(sender.Text))
            {
                m_xOk = true;
                sender.ColorMask = Vector4.One;
            }
            else
            {
                m_xOk = false;
                sender.ColorMask = Color.Red.ToVector4();
            }
            UpdateWarningLabel();
        }

        public void OnYChanged(MyGuiControlTextbox sender)
        {
            m_needsSyncY = true;
            if (IsCoordOk(sender.Text))
            {
                m_yOk = true;
                sender.ColorMask = Vector4.One;
            }
            else
            {
                m_yOk = false;
                sender.ColorMask = Color.Red.ToVector4();
            }
            UpdateWarningLabel();
        }

        public void OnZChanged(MyGuiControlTextbox sender)
        {
            m_needsSyncZ = true;
            if (IsCoordOk(sender.Text))
            {
                m_zOk = true;
                sender.ColorMask = Vector4.One;
            }
            else
            {
                m_zOk = false;
                sender.ColorMask = Color.Red.ToVector4();
            }
            UpdateWarningLabel();
        }

        public void OnSliderValueChanged(MyGuiControlSlider slider)
        {
            if (slider.Name == m_sliderHue.Name)
            {
                RefreshGPSColorControlsTooltips(clear: false, m_sliderHue);
            }
            if (slider.Name == m_sliderSaturation.Name)
            {
                RefreshGPSColorControlsTooltips(clear: false, m_sliderSaturation);
            }
            if (slider.Name == m_sliderValue.Name)
            {
                RefreshGPSColorControlsTooltips(clear: false, m_sliderValue);
            }
            OnColorChanged();
        }

        public void OnColorChanged()
        {
            m_needsSyncColor = true;
            if (m_tableIns.SelectedRow != null)
            {
                MyGuiControlTable.Cell cell = m_tableIns.SelectedRow.GetCell(0);
                if (cell != null)
                {
                    cell.TextColor = new Vector3((double)m_sliderHue.Value / 360.0, m_sliderSaturation.Value, m_sliderValue.Value).HSVtoColor();
                    m_hexSb.Clear();
                    m_hexSb.AppendFormat("#{0:X2}{1:X2}{2:X2}", cell.TextColor.Value.R, cell.TextColor.Value.G, cell.TextColor.Value.B);
                    m_textBoxHex.TextChanged -= HexTextboxOnTextChanged;
                    m_textBoxHex.TextChanged -= HexTextboxOnTextChanged;
                    m_textBoxHex.SetText(m_hexSb);
                    m_textBoxHex.TextChanged += HexTextboxOnTextChanged;
                }
            }
        }

        private void RefreshGPSColorControlsTooltips(bool clear = false, MyGuiControlSlider refreshOnlyThisSlider = null)
        {
            if (!clear)
            {
                m_textBoxHex.Tooltips.ToolTips[0].SetText(MyTexts.GetString(MySpaceTexts.GPSScreen_hexTooltip) + m_hexSb);
                m_textBoxHex.Tooltips.RecalculateSize();
                if (refreshOnlyThisSlider == null || refreshOnlyThisSlider == m_sliderHue)
                {
                    m_sliderHue.Tooltips.ToolTips[0].SetText(MyTexts.GetString(MySpaceTexts.GPSScreen_hueTooltip) + m_sliderHue.Value);
                    m_sliderHue.Tooltips.RecalculateSize();
                }
                if (refreshOnlyThisSlider == null || refreshOnlyThisSlider == m_sliderSaturation)
                {
                    m_sliderSaturation.Tooltips.ToolTips[0].SetText(MyTexts.GetString(MySpaceTexts.GPSScreen_saturationTooltip) + m_sliderSaturation.Value);
                    m_sliderSaturation.Tooltips.RecalculateSize();
                }
                if (refreshOnlyThisSlider == null || refreshOnlyThisSlider == m_sliderValue)
                {
                    m_sliderValue.Tooltips.ToolTips[0].SetText(MyTexts.GetString(MySpaceTexts.GPSScreen_valueTooltip) + m_sliderValue.Value);
                    m_sliderValue.Tooltips.RecalculateSize();
                }
            }
            else
            {
                m_textBoxHex.Tooltips.ToolTips[0].SetText("");
                m_textBoxHex.Tooltips.RecalculateSize();
                m_sliderHue.Tooltips.ToolTips[0].SetText("");
                m_sliderHue.Tooltips.RecalculateSize();
                m_sliderSaturation.Tooltips.ToolTips[0].SetText("");
                m_sliderSaturation.Tooltips.RecalculateSize();
                m_sliderValue.Tooltips.ToolTips[0].SetText("");
                m_sliderValue.Tooltips.RecalculateSize();
            }
        }

        private void UpdateWarningLabel()
        {
            if (m_nameOk && m_xOk && m_yOk && m_zOk)
            {
                m_labelSaveWarning.Visible = false;
                if (m_panelInsName.Enabled)
                {
                    m_buttonCopy.Enabled = true;
                }
            }
            else
            {
                m_labelSaveWarning.Visible = true;
                m_buttonCopy.Enabled = false;
            }
        }

        private bool trySync()
        {
            if (m_previousHash.HasValue && (m_needsSyncName || m_needsSyncDesc || m_needsSyncX || m_needsSyncY || m_needsSyncZ || m_needsSyncColor) && MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId) && IsNameOk(m_panelInsName.Text) && IsCoordOk(m_xCoord.Text) && IsCoordOk(m_yCoord.Text) && IsCoordOk(m_zCoord.Text) && MySession.Static.Gpss[MySession.Static.LocalPlayerId].TryGetValue(m_previousHash.Value, out var value))
            {
                if (m_needsSyncName)
                {
                    value.Name = m_panelInsName.Text;
                }
                if (m_needsSyncDesc)
                {
                    value.Description = m_panelInsDesc.Text.ToString();
                }
                StringBuilder stringBuilder = new StringBuilder();
                Vector3D coords = value.Coords;
                if (m_needsSyncX)
                {
                    m_xCoord.GetText(stringBuilder);
                    coords.X = Math.Round(double.Parse(stringBuilder.ToString(), CultureInfo.InvariantCulture), 2);
                }
                stringBuilder.Clear();
                if (m_needsSyncY)
                {
                    m_yCoord.GetText(stringBuilder);
                    coords.Y = Math.Round(double.Parse(stringBuilder.ToString(), CultureInfo.InvariantCulture), 2);
                }
                stringBuilder.Clear();
                if (m_needsSyncZ)
                {
                    m_zCoord.GetText(stringBuilder);
                    coords.Z = Math.Round(double.Parse(stringBuilder.ToString(), CultureInfo.InvariantCulture), 2);
                }
                if (m_needsSyncColor)
                {
                    value.GPSColor = new Vector3((double)m_sliderHue.Value / 360.0, m_sliderSaturation.Value, m_sliderValue.Value).HSVtoColor();
                }
                value.Coords = coords;
                m_syncedGps = value;
                MySession.Static.Gpss.SendModifyGpsRequest(MySession.Static.LocalPlayerId, value);
                return true;
            }
            return false;
        }

        private void OnButtonPressedNew(MyGuiControlButton sender)
        {
            trySync();
            MyGps gps = new MyGps();
            gps.Name = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewCoord_Name).ToString();
            gps.Description = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewCoord_Desc).ToString();
            gps.Coords = new Vector3D(0.0, 0.0, 0.0);
            gps.ShowOnHud = true;
            gps.DiscardAt = null;
            EnableEditBoxes(enable: false);
            MySession.Static.Gpss.SendAddGpsRequest(MySession.Static.LocalPlayerId, ref gps, 0L);
            m_searchBox.SearchText = string.Empty;
        }

        private void OnButtonPressedNewFromCurrent(MyGuiControlButton sender)
        {
            trySync();
            MyGps gps = new MyGps();
            MySession.Static.Gpss.GetNameForNewCurrent(m_NameBuilder, MySession.Static.LocalPlayerId, MySession.Static.LocalHumanPlayer.DisplayName);
            gps.Name = m_NameBuilder.ToString();
            gps.Description = MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewFromCurrent_Desc).ToString();
            Vector3D position = MySession.Static.LocalHumanPlayer.GetPosition();
            position.X = Math.Round(position.X, 2);
            position.Y = Math.Round(position.Y, 2);
            position.Z = Math.Round(position.Z, 2);
            gps.Coords = position;
            gps.ShowOnHud = true;
            gps.DiscardAt = null;
            EnableEditBoxes(enable: false);
            MySession.Static.Gpss.SendAddGpsRequest(MySession.Static.LocalPlayerId, ref gps, 0L);
            m_searchBox.SearchText = string.Empty;
        }

        private void PasteFromClipboard()
        {
            m_clipboardText = MyVRage.Platform.System.Clipboard;
        }

        private void OnButtonPressedNewFromClipboard(MyGuiControlButton sender)
        {
            Thread thread = new Thread((ThreadStart)delegate
            {
                PasteFromClipboard();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (!string.IsNullOrEmpty(m_clipboardText))
            {
                MySession.Static.Gpss.ScanText(m_clipboardText, MyTexts.Get(MySpaceTexts.TerminalTab_GPS_NewFromClipboard_Desc));
            }
            m_searchBox.SearchText = string.Empty;
        }

        private void OnButtonPressedDelete(MyGuiControlButton sender)
        {
            if (m_tableIns.SelectedRow != null)
            {
                Delete();
            }
        }

        private void Delete()
        {
            MySession.Static.Gpss.SendDeleteGpsRequest(MySession.Static.LocalPlayerId, ((MyGps)m_tableIns.SelectedRow.UserData).GetHashCode());
            EnableEditBoxes(enable: false);
            SetDeleteButtonEnabled(enabled: false);
            PopulateList();
        }

        public void OnDelKeyPressed()
        {
            if (m_tableIns.SelectedRow != null && m_tableIns.HasFocus)
            {
                Delete();
            }
        }

        private void OnButtonPressedCopy(MyGuiControlButton sender)
        {
            if (m_tableIns.SelectedRow != null)
            {
                if (trySync())
                {
                    m_syncedGps.ToClipboard();
                }
                else
                {
                    ((MyGps)m_tableIns.SelectedRow.UserData).ToClipboard();
                }
            }
        }

        private void OnGpsAdded(long id, int hash)
        {
            if (id != MySession.Static.LocalPlayerId)
            {
                return;
            }
            for (int i = 0; i < m_tableIns.RowsCount; i++)
            {
                MyGps myGps = (MyGps)m_tableIns.GetRow(i).UserData;
                if (myGps.GetHashCode() == hash)
                {
                    m_tableIns.SelectedRowIndex = i;
                    if (m_tableIns.SelectedRow != null)
                    {
                        EnableEditBoxes(enable: true);
                        SetDeleteButtonEnabled(enabled: true);
                        FillRight((MyGps)m_tableIns.SelectedRow.UserData);
                        m_tableIns.GetRow(i).GetCell(0).TextColor = (myGps.DiscardAt.HasValue ? Color.Gray : (myGps.ShowOnHud ? myGps.GPSColor : Color.White));
                    }
                    break;
                }
            }
        }

        private void OnInsChanged(long id, int hash)
        {
            if (id != MySession.Static.LocalPlayerId)
            {
                return;
            }
            if (m_tableIns.SelectedRow != null && ((MyGps)m_tableIns.SelectedRow.UserData).GetHashCode() == hash)
            {
                FillRight();
            }
            for (int i = 0; i < m_tableIns.RowsCount; i++)
            {
                if (((MyGps)m_tableIns.GetRow(i).UserData).GetHashCode() == hash)
                {
                    MyGuiControlTable.Cell cell = m_tableIns.GetRow(i).GetCell(0);
                    if (cell != null)
                    {
                        MyGps myGps = (MyGps)m_tableIns.GetRow(i).UserData;
                        cell.TextColor = (myGps.DiscardAt.HasValue ? Color.Gray : (myGps.ShowOnHud ? myGps.GPSColor : Color.White));
                        cell.Text.Clear().Append(((MyGps)m_tableIns.GetRow(i).UserData).Name);
                    }
                    break;
                }
            }
        }

        private void OnListChanged(long id)
        {
            if (id == MySession.Static.LocalPlayerId)
            {
                PopulateList();
            }
        }

        private void OnShowOnHudChecked(MyGuiControlCheckbox sender)
        {
            if (m_tableIns.SelectedRow != null)
            {
                MyGps myGps = m_tableIns.SelectedRow.UserData as MyGps;
                myGps.ShowOnHud = sender.IsChecked;
                if (!sender.IsChecked && myGps.AlwaysVisible)
                {
                    myGps.AlwaysVisible = false;
                    MyGuiControlCheckbox checkInsShowOnHud = m_checkInsShowOnHud;
                    checkInsShowOnHud.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Remove(checkInsShowOnHud.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
                    m_checkInsShowOnHud.IsChecked = false;
                    MyGuiControlCheckbox checkInsShowOnHud2 = m_checkInsShowOnHud;
                    checkInsShowOnHud2.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Combine(checkInsShowOnHud2.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
                }
                if (!trySync())
                {
                    MySession.Static.Gpss.SendChangeShowOnHudRequest(MySession.Static.LocalPlayerId, myGps.Hash, sender.IsChecked);
                }
            }
        }

        private void OnAlwaysVisibleChecked(MyGuiControlCheckbox sender)
        {
            if (m_tableIns.SelectedRow != null)
            {
                MyGps myGps = m_tableIns.SelectedRow.UserData as MyGps;
                myGps.AlwaysVisible = sender.IsChecked;
                myGps.ShowOnHud = myGps.ShowOnHud || myGps.AlwaysVisible;
                MyGuiControlCheckbox checkInsShowOnHud = m_checkInsShowOnHud;
                checkInsShowOnHud.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Remove(checkInsShowOnHud.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
                m_checkInsShowOnHud.IsChecked = m_checkInsShowOnHud.IsChecked || sender.IsChecked;
                MyGuiControlCheckbox checkInsShowOnHud2 = m_checkInsShowOnHud;
                checkInsShowOnHud2.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Combine(checkInsShowOnHud2.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
                if (!trySync())
                {
                    MySession.Static.Gpss.SendChangeAlwaysVisibleRequest(MySession.Static.LocalPlayerId, myGps.Hash, sender.IsChecked);
                }
            }
        }

        private void FillRight()
        {
            if (m_tableIns.SelectedRow != null)
            {
                FillRight((MyGps)m_tableIns.SelectedRow.UserData);
            }
            else
            {
                ClearRight();
            }
            m_nameOk = (m_xOk = (m_yOk = (m_zOk = true)));
            UpdateWarningLabel();
        }

        private void HexTextboxOnFocusChanged(MyGuiControlBase obj, bool state)
        {
            MyGuiControlTextbox obj2;
            if (!state && (obj2 = obj as MyGuiControlTextbox) != null)
            {
                HexTextboxOnTextChanged(obj2);
            }
        }

        private Color GetCurrentColor()
        {
            return new Vector3(m_sliderHue.Value / 360f, m_sliderSaturation.Value, m_sliderValue.Value).HSVtoColor();
        }

        private void HexTextboxOnTextChanged(MyGuiControlTextbox obj)
        {
            if (MySession.Static.LocalHumanPlayer == null)
            {
                return;
            }
            UnhookSyncEvents();
            Match match = CheckHexTextbox();
            if (!match.Success || match.Length == 0)
            {
                HookSyncEvents();
                return;
            }
            string text = match.Value;
            if (text.Length > 6)
            {
                text = text.Substring(1);
            }
            byte r = byte.Parse(text.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber);
            Vector3 vector = new Color(r, g, b).ColorToHSV();
            m_sliderHue.Value = vector.X * 360f;
            m_sliderSaturation.Value = vector.Y;
            m_sliderValue.Value = vector.Z;
            OnColorChanged();
            HookSyncEvents();
        }

        private Match CheckHexTextbox()
        {
            m_hexSb.Clear();
            m_textBoxHex.GetText(m_hexSb);
            Match match = HEX_REGEX.Match(m_hexSb.ToString());
            if (!match.Success || match.Length == 0)
            {
                Color currentColor = GetCurrentColor();
                m_hexSb.Clear();
                m_hexSb.AppendFormat("#{0:X2}{1:X2}{2:X2}", currentColor.R, currentColor.G, currentColor.B);
                m_textBoxHex.ColorMask = Vector4.One;
                m_textBoxHex.ColorMask = Color.Red.ToVector4();
                return match;
            }
            if (m_textBoxHex.ColorMask == Color.Red.ToVector4())
            {
                m_textBoxHex.ColorMask = m_buttonAdd.ColorMask;
            }
            return match;
        }

        private void FillRight(MyGps ins)
        {
            UnhookSyncEvents();
            EnableEditBoxes(ins.EntityId == 0L && !ins.IsContainerGPS);
            m_panelInsName.SetText(new StringBuilder(ins.Name));
            m_panelInsDesc.Text = new StringBuilder(ins.Description);
            m_xCoord.SetText(new StringBuilder(ins.Coords.X.ToString("F2", CultureInfo.InvariantCulture)));
            m_yCoord.SetText(new StringBuilder(ins.Coords.Y.ToString("F2", CultureInfo.InvariantCulture)));
            m_zCoord.SetText(new StringBuilder(ins.Coords.Z.ToString("F2", CultureInfo.InvariantCulture)));
            Vector3 vector = ins.GPSColor.ColorToHSV();
            m_hexSb.Clear();
            m_hexSb.AppendFormat("#{0:X2}{1:X2}{2:X2}", ins.GPSColor.R, ins.GPSColor.G, ins.GPSColor.B);
            m_textBoxHex.TextChanged -= HexTextboxOnTextChanged;
            m_textBoxHex.SetText(m_hexSb);
            CheckHexTextbox();
            m_textBoxHex.TextChanged += HexTextboxOnTextChanged;
            m_sliderHue.Value = vector.X * 360f;
            m_sliderSaturation.Value = vector.Y;
            m_sliderValue.Value = vector.Z;
            MyGuiControlCheckbox checkInsShowOnHud = m_checkInsShowOnHud;
            checkInsShowOnHud.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Remove(checkInsShowOnHud.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
            m_checkInsShowOnHud.IsChecked = ins.ShowOnHud;
            MyGuiControlCheckbox checkInsShowOnHud2 = m_checkInsShowOnHud;
            checkInsShowOnHud2.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Combine(checkInsShowOnHud2.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
            MyGuiControlCheckbox checkInsAlwaysVisible = m_checkInsAlwaysVisible;
            checkInsAlwaysVisible.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Remove(checkInsAlwaysVisible.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnAlwaysVisibleChecked));
            m_checkInsAlwaysVisible.IsChecked = ins.AlwaysVisible;
            MyGuiControlCheckbox checkInsAlwaysVisible2 = m_checkInsAlwaysVisible;
            checkInsAlwaysVisible2.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Combine(checkInsAlwaysVisible2.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnAlwaysVisibleChecked));
            m_previousHash = ins.Hash;
            RefreshGPSColorControlsTooltips();
            HookSyncEvents();
            m_needsSyncName = false;
            m_needsSyncDesc = false;
            m_needsSyncX = false;
            m_needsSyncY = false;
            m_needsSyncZ = false;
            m_needsSyncColor = false;
            m_panelInsName.ColorMask = Vector4.One;
            m_xCoord.ColorMask = Vector4.One;
            m_yCoord.ColorMask = Vector4.One;
            m_zCoord.ColorMask = Vector4.One;
            m_nameOk = (m_xOk = (m_yOk = (m_zOk = true)));
            UpdateWarningLabel();
        }

        private void ClearRight()
        {
            UnhookSyncEvents();
            StringBuilder text = new StringBuilder("");
            m_panelInsName.SetText(text);
            m_panelInsDesc.Clear();
            m_xCoord.SetText(text);
            m_yCoord.SetText(text);
            m_zCoord.SetText(text);
            m_textBoxHex.TextChanged -= HexTextboxOnTextChanged;
            m_textBoxHex.SetText(text);
            m_textBoxHex.TextChanged += HexTextboxOnTextChanged;
            m_sliderValue.Value = 0f;
            m_sliderSaturation.Value = 0f;
            m_sliderHue.Value = 0f;
            m_checkInsShowOnHud.IsChecked = false;
            m_checkInsAlwaysVisible.IsChecked = false;
            m_previousHash = null;
            RefreshGPSColorControlsTooltips(clear: true);
            HookSyncEvents();
            m_needsSyncName = false;
            m_needsSyncDesc = false;
            m_needsSyncX = false;
            m_needsSyncY = false;
            m_needsSyncZ = false;
            m_needsSyncColor = false;
        }

        private void LabelClipboardGamepadHelp_VisibleChanged(object sender, bool isVisible)
        {
            m_labelClipboardGamepadHelp.TextEnum = (isVisible ? MySpaceTexts.TerminalTab_GPS_CopyToClipboard_GamepadHelp : MyStringId.NullOrEmpty);
        }

        public void Close()
        {
            trySync();
            if (m_tableIns != null)
            {
                ClearList();
                m_tableIns.ItemSelected -= OnTableItemSelected;
                m_tableIns.ItemDoubleClicked -= OnTableDoubleclick;
            }
            m_syncedGps = null;
            MySession.Static.Gpss.GpsAdded -= OnGpsAdded;
            MySession.Static.Gpss.GpsChanged -= OnInsChanged;
            MySession.Static.Gpss.ListChanged -= OnListChanged;
            UnhookSyncEvents();
            MyGuiControlCheckbox checkInsShowOnHud = m_checkInsShowOnHud;
            checkInsShowOnHud.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Remove(checkInsShowOnHud.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnShowOnHudChecked));
            MyGuiControlCheckbox checkInsAlwaysVisible = m_checkInsAlwaysVisible;
            checkInsAlwaysVisible.IsCheckedChanged = (Action<MyGuiControlCheckbox>)Delegate.Remove(checkInsAlwaysVisible.IsCheckedChanged, new Action<MyGuiControlCheckbox>(OnAlwaysVisibleChecked));
            m_buttonAdd.ButtonClicked -= OnButtonPressedNew;
            m_buttonAddFromClipboard.ButtonClicked -= OnButtonPressedNewFromClipboard;
            m_buttonAddCurrent.ButtonClicked -= OnButtonPressedNewFromCurrent;
            m_buttonDelete.ButtonClicked -= OnButtonPressedDelete;
            m_buttonCopy.ButtonClicked -= OnButtonPressedCopy;
        }

        public void HandleInput()
        {
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Delete))
            {
                OnDelKeyPressed();
            }
            if (MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.BUTTON_Y))
            {
                OnButtonPressedDelete(null);
            }
            if (MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.BUTTON_X))
            {
                bool flag = MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MODIF_L, MyControlStateType.PRESSED);
                bool flag2 = MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.MODIF_R, MyControlStateType.PRESSED);
                if (flag && !flag2)
                {
                    OnButtonPressedNew(null);
                }
                else if (flag2 && !flag)
                {
                    OnButtonPressedNewFromClipboard(null);
                }
                else if (!flag2 && !flag)
                {
                    OnButtonPressedNewFromCurrent(null);
                }
            }
            if (m_tableIns != null && m_tableIns.HasFocus && MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.ACCEPT))
            {
                ToggleShowOnHud(m_tableIns);
            }
            m_buttonAdd.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_buttonAddCurrent.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_buttonAddFromClipboard.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_buttonDelete.Visible = !MyInput.Static.IsJoystickLastUsed;
            m_labelClipboardGamepadHelp.Visible = m_buttonCopy.Enabled && MyInput.Static.IsJoystickLastUsed;
        }
    }
}

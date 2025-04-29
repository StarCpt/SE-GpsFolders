using HarmonyLib;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GpsFolders.Wrappers
{
    public class MyTerminalGpsControllerWrapper
    {
        public static Type WrappedType => _type;

        private static readonly Type _type = AccessTools.TypeByName("Sandbox.Game.Screens.Terminal.MyTerminalGpsController");
        private static readonly Action<object> _populateListMethod = _type.Method(nameof(PopulateList)).CreateInvoker();
        private static readonly Action<object> _fillRight1Method = _type.Method(nameof(FillRight), Array.Empty<Type>()).CreateInvoker();
        private static readonly Action<object, MyGps> _fillRight2Method = _type.Method(nameof(FillRight), typeof(MyGps)).CreateInvoker();
        private static readonly Action<object> _clearListMethod = _type.Method(nameof(ClearList)).CreateInvoker();
        private static readonly Action<object, bool, bool, bool> _enableEditBoxesMethod = _type.Method(nameof(EnableEditBoxes)).CreateInvoker();
        private static readonly Action<object, bool> _setDeleteButtonEnabledMethod = _type.Method(nameof(SetDeleteButtonEnabled)).CreateInvoker();
        private static readonly Action<object, MyGuiControlListbox> _setEnabledStatesMethod = _type.Method(nameof(SetEnabledStates)).CreateInvoker();
        private static readonly Action<object, MyGps, string> _addTolistMethod = _type.Method(nameof(AddToList)).CreateInvoker();

        private readonly object _instance;

        public MyTerminalGpsControllerWrapper(object instance)
        {
            if (instance.GetType() != _type)
            {
                throw new ArgumentException($"Must be a type of {_type}", nameof(instance));
            }

            _instance = instance;
        }

        public void PopulateList() => _populateListMethod.Invoke(_instance);
        public void FillRight() => _fillRight1Method.Invoke(_instance);
        public void FillRight(MyGps gps) => _fillRight2Method.Invoke(_instance, gps);
        public void ClearList() => _clearListMethod.Invoke(_instance);
        public void EnableEditBoxes(bool enable, bool forceEnableColorSelection = false, bool isGpsReadOnly = false) => _enableEditBoxesMethod.Invoke(_instance, enable, forceEnableColorSelection, isGpsReadOnly);
        public void SetDeleteButtonEnabled(bool enabled) => _setDeleteButtonEnabledMethod.Invoke(_instance, enabled);
        public void SetEnabledStates(MyGuiControlListbox senderListbox) => _setEnabledStatesMethod.Invoke(_instance, senderListbox);
        public void AddToList(MyGps gps, string nameOverride = null) => _addTolistMethod.Invoke(_instance, gps, nameOverride);
    }
}

// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Security.Permissions;
using System.Windows.Forms.Design;
using NanoByte.Common.Controls;

namespace NanoByte.Common.Values.Design
{
    /// <summary>
    /// An editor that can be associated with <see cref="TimeSpan"/> properties. Uses <see cref="TimeSpanControl"/>.
    /// </summary>
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public class TimeSpanEditor : UITypeEditor
    {
        /// <inheritdoc/>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.DropDown;

        /// <inheritdoc/>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            #region Sanity checks
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (value == null) throw new ArgumentNullException(nameof(value));
            #endregion

            if (value.GetType() != typeof(TimeSpan)) return value;

            var editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (editorService == null) return value;

            var picker = new TimeSpanControl {Value = (TimeSpan)value};
            editorService.DropDownControl(picker);
            value = picker.Value;

            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FishBench
{
    static class ControlAsync
    {
        public static void SetAsync(this Control control, string field, object val)
        {
            control.BeginInvoke(new Action<object, string, object>(setValue), new object[] { control, field, val });
        }
        private static void setValue(object control, string field, object val)
        {
            control.GetType().GetProperty(field).SetValue(control, val);
        }
    }
}

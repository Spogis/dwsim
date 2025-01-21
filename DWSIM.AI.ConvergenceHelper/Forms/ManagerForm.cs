using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using DWSIM.ExtensionMethods;
using DWSIM.ExtensionMethods.Eto;
using ext = DWSIM.UI.Shared.Common;
using DWSIM.UI.Shared;

namespace DWSIM.AI.ConvergenceHelper.Forms
{
    public static class ManagerForm
    {

        public static void DisplayConfigForm()
        {

            var c1 = ext.GetDefaultContainer();
            c1.Tag = "Settings";

            c1.CreateAndAddCheckBoxRow("Enable AI Convergence Helper", GlobalSettings.Settings.ConvergenceHelperEnabled,
                (chk, e) => GlobalSettings.Settings.ConvergenceHelperEnabled = chk.Checked.GetValueOrDefault());

            c1.CreateAndAddCheckBoxRow("Use ANN Model Outputs on Errors", GlobalSettings.Settings.ConvergenceHelperSolutionOnErrorEnabled,
                (chk, e) => GlobalSettings.Settings.ConvergenceHelperSolutionOnErrorEnabled = chk.Checked.GetValueOrDefault());

            var c2 = ext.GetDefaultContainer();
            c2.Tag = "Models";

            var c3 = ext.GetDefaultContainer();
            c3.Tag = "Data";

            var form = Extensions2.GetTabbedForm("AI Convergence Helper Manager", 1024, 768, new DynamicLayout[] { c1, c2, c3 });
            form.SetFontAndPadding();
            form.Show();
            form.Center();

        }



    }
}

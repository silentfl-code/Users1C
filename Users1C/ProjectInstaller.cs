using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;

namespace Users1C
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
#if NEEDLOG
            if (System.Diagnostics.EventLog.Exists("Users1C"))
            {
                System.Diagnostics.EventLog.Delete("Users1C");
            }
#endif
            InitializeComponent();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Framework.Application.Model
{
    public enum eModule_Type
    {
        UNKNOWN,
        CS,
        TS,
        DS,
        ES
    }

    public class AppModuleConfig
    {
        public string Module_Id { get; set; }
        public string Module_Name { get; set; }
        public eModule_Type Module_Type { get; set; }
        public string Path { get; set; }
        public string config_00 { get; set; }
        public string config_01 { get; set; }
        public string config_02 { get; set; }
        public string config_03 { get; set; }
        public string config_04 { get; set; }
        public string config_05 { get; set; }
        public string config_06 { get; set; }
        public string config_07 { get; set; }
        public string config_08 { get; set; }
        public string config_09 { get; set; }

    } 
}

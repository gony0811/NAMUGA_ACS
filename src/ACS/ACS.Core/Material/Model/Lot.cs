using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Material.Model
{
    public class Lot : NamedEntity
    {
        private string grade;
        private string priority;
        private string productType;
        private string productSpecName;
        private string productSpecVersion;
        private int productQuantity;
        private string processFlowName;
        private string processFlowVersion;
        private string processOperationName;
        private string processOperationVersion;
        private string nextProcessOperationName;
        private string nextProcessOperationVersion;
        private string machineName;
        private string nextMachineName;
        private string additionalInfo = "";

        public string Grade { get { return grade; } set { grade = value; } }  
        public string Priority { get { return priority; } set { priority = value; } }  
        public string ProductType { get { return productType; } set { productType = value; } }  
        public string ProductSpecName { get { return productSpecName; } set { productSpecName = value; } }  
        public string ProductSpecVersion { get { return productSpecVersion; } set { productSpecVersion = value; } }  
        public int ProductQuantity { get { return productQuantity; } set { productQuantity = value; } }  
        public string ProcessFlowName { get { return processFlowName; } set { processFlowName = value; } }  
        public string ProcessFlowVersion { get { return processFlowVersion; } set { processFlowVersion = value; } }  
        public string ProcessOperationName { get { return processOperationName; } set { processOperationName = value; } }  
        public string ProcessOperationVersion { get { return processOperationVersion; } set { processOperationVersion = value; } }  
        public string NextProcessOperationName { get { return nextProcessOperationName; } set { nextProcessOperationName = value; } }  
        public string NextProcessOperationVersion { get { return nextProcessOperationVersion; } set { nextProcessOperationVersion = value; } }  
        public string MachineName { get { return machineName; } set { machineName = value; } }  
        public string NextMachineName { get { return nextMachineName; } set { nextMachineName = value; } }
        public string AdditionalInfo { get { return additionalInfo; } set { additionalInfo = value; } }  

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("lot{");

            sb.Append(", grade=").Append(this.grade);
            sb.Append(", priority=").Append(this.priority);
            sb.Append(", productType=").Append(this.productType);
            sb.Append(", productSpecName=").Append(this.productSpecName);
            sb.Append(", productSpecVersion=").Append(this.productSpecVersion);
            sb.Append(", productQuantity=").Append(this.productQuantity);
            sb.Append(", processFlowName=").Append(this.processFlowName);
            sb.Append(", processFlowVersion=").Append(this.processFlowVersion);
            sb.Append(", processOperationName=").Append(this.processOperationName);
            sb.Append(", processOperationVersion=").Append(this.processOperationVersion);
            sb.Append(", nextProcessOperationName=").Append(this.nextProcessOperationName);
            sb.Append(", nextProcessOperationVersion=").Append(this.nextProcessOperationVersion);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", nextMachineName=").Append(this.nextMachineName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}

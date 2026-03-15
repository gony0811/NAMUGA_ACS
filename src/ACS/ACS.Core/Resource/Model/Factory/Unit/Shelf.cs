using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACS.Core.Resource.Model.Factory.Unit
{
    public class Shelf : ZoneUnit
    {
        public static string STATUS_EMPTY = "0";
        public static string STATUS_OCCUPIED = "1";
        public static string STATUS_RESERVED = "2";
        public static string shelfDiscriminator = "_";
        private int scanCount;
        private int positionX;
        private int positionY;
        private int positionZ;

        public int PositionX
        {
            get { return positionX; }
            set { positionX = value; }
        }

        public int PositionY
        {
            get { return positionY; }
            set { positionY = value; }
        }

        public int PositionZ
        {
            get { return positionZ; }
            set { positionZ = value; }
        }

        public int ScanCount
        {
            get { return scanCount; }
            set { scanCount = value; }
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("shelf{");
            sb.Append("id=").Append(this.Id);
            sb.Append(", name=").Append(base.Name);
            sb.Append(", machineName=").Append(this.machineName);
            sb.Append(", type=").Append(this.type);
            sb.Append(", state=").Append(this.state);
            sb.Append(", processingState=").Append(this.processingState);
            sb.Append(", subState=").Append(this.subState);
            sb.Append(", maxCapacity=").Append(this.maxCapacity);
            sb.Append(", currentCapacity=").Append(this.currentCapacity);
            sb.Append(", occupied=").Append(this.occupied);
            sb.Append(", reserved=").Append(this.reserved);
            sb.Append(", banned=").Append(this.banned);
            sb.Append(", lastTransferTime=").Append(this.lastTransferTime);
            sb.Append(", zoneName=").Append(this.zoneName);
            sb.Append("}");
            return sb.ToString();
        }
    }
}

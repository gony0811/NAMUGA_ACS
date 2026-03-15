using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACS.Core.Base;

namespace ACS.Core.Route.Model
{
    public class RouteInfo : Entity
    {
        private ArrayList routesAvailable = new ArrayList();
        private ArrayList routesUnavailable = new ArrayList();
        private ArrayList routesBanned = new ArrayList();
        private ArrayList availableInternalRoutes = new ArrayList();
        private bool existRoute = true;
        private bool useInternalRoute = false;
        private int maxHoppingCount;
        private int maxTotalCost;
        private bool useDynamicLoad;
        private Dictionary<string, string> singleNodes;
        private bool useHeuristicDelay;
        private Dictionary<string, string> tripleNodes;
        private Route optimalRouteAvailable;
        private Route optimalRouteUnavailable;
        private int toleranceHoppingPercent = 0;
        private int toleranceCostPercent = 0;

        public ArrayList RoutesAvailable { get { return routesAvailable; } set { routesAvailable = value; } }
        public ArrayList RoutesUnavailable { get { return routesUnavailable; } set { routesUnavailable = value; } }
        public ArrayList RoutesBanned { get { return routesBanned; } set { routesBanned = value; } }
        public ArrayList AvailableInternalRoutes { get { return availableInternalRoutes; } set { availableInternalRoutes = value; } }
        public bool ExistRoute { get { return existRoute; } set { existRoute = value; } }
        public bool UseInternalRoute { get { return useInternalRoute; } set { useInternalRoute = value; } }
        public int MaxHoppingCount { get { return maxHoppingCount; } set { maxHoppingCount = value; } }
        public int MaxTotalCost { get { return maxTotalCost; } set { maxTotalCost = value; } }
        public bool UseDynamicLoad { get { return useDynamicLoad; } set { useDynamicLoad = value; } }
        public Dictionary<string, string> SingleNodes { get { return singleNodes; } set { singleNodes = value; } }
        public bool UseHeuristicDelay { get { return useHeuristicDelay; } set { useHeuristicDelay = value; } }
        public Dictionary<string, string> TripleNodes { get { return tripleNodes; } set { tripleNodes = value; } }
        public Route OptimalRouteAvailable { get { return optimalRouteAvailable; } set { optimalRouteAvailable = value; } }
        public Route OptimalRouteUnavailable { get { return optimalRouteUnavailable; } set { optimalRouteUnavailable = value; } }
        public int ToleranceHoppingPercent { get { return toleranceHoppingPercent; } set { toleranceHoppingPercent = value; } }
        public int ToleranceCostPercent { get { return toleranceCostPercent; } set { toleranceCostPercent = value; } }

        public int AddRouteAvailable(Route route)
        {
            return this.routesAvailable.Add(route);
        }

        public int AddRouteUnavailable(Route route)
        {
            return this.routesUnavailable.Add(route);
        }

        public int AddRouteBanned(Route route)
        {
            return this.routesBanned.Add(route);
        }

        public void AddRoutesAvailable(ArrayList routesAvailable)
        {
            this.routesAvailable.AddRange(routesAvailable);
        }

        public void AddRoutesUnavailable(ArrayList routesUnavailable)
        {
            this.routesUnavailable.AddRange(routesUnavailable);
        }

        public void AddRoutesBanned(ArrayList routesBanned)
        {
            this.routesBanned.AddRange(routesBanned);
        }

        public void AddAvailableInternalRoutes(ArrayList internalRoutes)
        {
            this.availableInternalRoutes.AddRange(internalRoutes);
        }

        public void AddAvailableInternalRoute(InternalRoute internalRoute)
        {
            this.availableInternalRoutes.Add(internalRoute);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("routeInfo{");
            sb.Append("routesAvailable{" + this.routesAvailable.Count + "}=").Append(this.routesAvailable);
            sb.Append(", routesUnavailable{" + this.routesUnavailable.Count + "}=").Append(this.routesUnavailable);
            sb.Append(", routesBanned{" + this.routesBanned.Count + "}=").Append(this.routesBanned);
            sb.Append(", availableInternalRoutes{" + this.availableInternalRoutes.Count + "}=").Append(this.availableInternalRoutes);
            sb.Append(", existRoute=").Append(this.existRoute);
            sb.Append(", useInternalRoute=").Append(this.useInternalRoute);
            sb.Append("}");
            return sb.ToString();
        }
    }
}

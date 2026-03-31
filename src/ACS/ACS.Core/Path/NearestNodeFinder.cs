using System;
using System.Collections.Generic;
using ACS.Core.Path.Model;

namespace ACS.Core.Path
{
    /// <summary>
    /// AMR Pose(X, Y) 좌표에서 가장 가까운 Node를 찾는 유틸리티.
    /// Euclidean distance 기반 선형 탐색 (노드 수가 적어 O(N) 충분).
    /// </summary>
    public class NearestNodeFinder
    {
        /// <summary>
        /// 주어진 Pose 좌표에서 가장 가까운 Node를 찾는다.
        /// </summary>
        /// <param name="nodes">전체 Node 목록</param>
        /// <param name="poseX">AMR X 좌표 (meters)</param>
        /// <param name="poseY">AMR Y 좌표 (meters)</param>
        /// <param name="thresholdMeters">최대 허용 거리 (meters). 이 거리 초과 시 null 반환</param>
        /// <returns>가장 가까운 NodeEx, threshold 초과 시 null</returns>
        public NodeEx FindNearestNode(List<NodeEx> nodes, float poseX, float poseY, double thresholdMeters)
        {
            if (nodes == null || nodes.Count == 0)
                return null;

            NodeEx nearestNode = null;
            double minDistanceSq = double.MaxValue;
            double thresholdSq = thresholdMeters * thresholdMeters;

            foreach (var node in nodes)
            {
                double dx = node.Xpos - poseX;
                double dy = node.Ypos - poseY;
                double distanceSq = dx * dx + dy * dy;

                if (distanceSq < minDistanceSq)
                {
                    minDistanceSq = distanceSq;
                    nearestNode = node;
                }
            }

            if (minDistanceSq > thresholdSq)
                return null;

            return nearestNode;
        }
    }
}

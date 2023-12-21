#if CESIUM_FOR_UNITY
using CesiumForUnity;
using System;
using UnityEngine;
using Color = UnityEngine.Color;

namespace PlateauToolkit.Maps
{
    /// <summary>
    /// An implementation of <see cref="CesiumTileExcluder" /> with bounds to exclude tiles.
    /// </summary>
    [ExecuteAlways]
    public class CesiumBoundsTileExcluder : CesiumTileExcluder
    {
        enum ExclusionMethod
        {
            NotIntersectsWithArea,
            ContainsCompletely,
        }

        /// <summary>
        /// If inverting the detection.
        /// </summary>
        [SerializeField] ExclusionMethod m_Method;

        [SerializeField] Transform m_PositionTransform;
        [SerializeField] Vector2 m_Size;

        Bounds m_ExcluderBounds;

        static bool CompletelyContains(Bounds bounds, Bounds other)
        {
            if (bounds.min.x < other.min.x && bounds.min.z < other.min.z &&
                bounds.max.x > other.max.x && bounds.max.z > other.max.z)
            {
                return true;
            }

            return false;
        }

        void Update()
        {
            m_ExcluderBounds.size = new Vector3(m_Size.x, 10000, m_Size.y);
            if (m_PositionTransform == null)
            {
                return;
            }

            m_ExcluderBounds.center = m_PositionTransform.position;
        }

        public override bool ShouldExclude(Cesium3DTile tile)
        {
            if (!enabled)
            {
                return false;
            }

            if (m_PositionTransform == null)
            {
                return false;
            }

            switch (m_Method)
            {
                case ExclusionMethod.NotIntersectsWithArea:
                    return !m_ExcluderBounds.Intersects(tile.bounds);
                case ExclusionMethod.ContainsCompletely:
                    return CompletelyContains(m_ExcluderBounds, tile.bounds);
                default:
                    Debug.LogError($"{nameof(m_Method)} is invalid");
                    return false;
            }
        }

        void OnDrawGizmos()
        {
            if (m_PositionTransform == null)
            {
                return;
            }

            Gizmos.color = new Color(0.2f, 0.4f, 1f, 0.3f);
            Gizmos.DrawCube(m_PositionTransform.position, new Vector3(m_Size.x, 0, m_Size.y));
        }
    }
}
#endif
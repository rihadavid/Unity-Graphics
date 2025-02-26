using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HairData : HDTargetData
    {
        public enum MaterialType
        {
            KajiyaKay,
            Marschner
        }

        public enum ColorParameterization
        {
            BaseColor,
            Melanin,
            Absorption
        }

        public enum GeometryType
        {
            Cards,
            Strands
        }

        public enum ScatteringMode
        {
            Approximate,
            Advanced
        }

        [SerializeField]
        MaterialType m_MaterialType;
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        [SerializeField]
        ScatteringMode m_ScatteringMode;

        public ScatteringMode scatteringMode
        {
            get => m_ScatteringMode;
            set => m_ScatteringMode = value;
        }

        [SerializeField]
        ColorParameterization m_ColorParameterization = ColorParameterization.BaseColor;

        public ColorParameterization colorParameterization
        {
            get => m_ColorParameterization;
            set => m_ColorParameterization = value;
        }

        [SerializeField]
        GeometryType m_GeometryType;

        public GeometryType geometryType
        {
            get => m_GeometryType;
            set => m_GeometryType = value;
        }
    }
}

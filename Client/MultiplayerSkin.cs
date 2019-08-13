using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Linq;

namespace PocceMod.Client
{
    [Serializable]
    public class MultiplayerSkin
    {
        public class HeadOverlay
        {
            public HeadOverlay(int style, int colorType, int firstColor, int secondColor, float opacity)
            {
                Style = style;
                ColorType = colorType;
                FirstColor = firstColor;
                SecondColor = secondColor;
                Opacity = opacity;
            }

            public int Style { get; set; }
            public int ColorType { get; set; }
            public int FirstColor { get; set; }
            public int SecondColor { get; set; }
            public float Opacity { get; set; }

            public override bool Equals(object value)
            {
                var other = value as HeadOverlay;
                return other != null &&
                    Style == other.Style &&
                    ColorType == other.ColorType &&
                    FirstColor == other.FirstColor &&
                    SecondColor == other.SecondColor &&
                    Opacity == other.Opacity;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        private const int HeadOverlaysCount = 12;
        private const int FaceFeaturesCount = 20;
        private readonly int _hairColor;
        private readonly int _hairHighlightColor;
        private readonly int _shapeFirstID;
        private readonly int _shapeSecondID;
        private readonly int _shapeThirdID;
        private readonly int _skinFirstID;
        private readonly int _skinSecondID;
        private readonly int _skinThirdID;
        private readonly float _shapeMix;
        private readonly float _skinMix;
        private readonly float _thirdMix;
        private readonly bool _isParent;
        private readonly HeadOverlay[] _headOverlays;
        private readonly float[] _faceFeatures;

        public MultiplayerSkin(int ped)
        {
            _hairColor = API.GetPedHairColor(ped);
            _hairHighlightColor = API.GetPedHairHighlightColor(ped);

            var data = new Ped(ped).GetHeadBlendData();
            _shapeFirstID = data.FirstFaceShape;
            _shapeSecondID = data.SecondFaceShape;
            _shapeThirdID = data.ThirdFaceShape;
            _skinFirstID = data.FirstSkinTone;
            _skinSecondID = data.SecondSkinTone;
            _skinThirdID = data.ThirdSkinTone;
            _shapeMix = data.ParentFaceShapePercent;
            _skinMix = data.ParentSkinTonePercent;
            _thirdMix = data.ParentThirdUnkPercent;
            _isParent = data.IsParentInheritance;

            _headOverlays = new HeadOverlay[HeadOverlaysCount];
            _faceFeatures = new float[FaceFeaturesCount];

            for (int i = 0; i < HeadOverlaysCount; ++i)
            {
                int style = 0;
                int colorType = 0;
                int firstColor = 0;
                int secondColor = 0;
                float opacity = 0f;
                API.GetPedHeadOverlayData(ped, i, ref style, ref colorType, ref firstColor, ref secondColor, ref opacity);
                _headOverlays[i] = new HeadOverlay(style, colorType, firstColor, secondColor, opacity);
            }

            for (int i = 0; i < FaceFeaturesCount; ++i)
            {
                _faceFeatures[i] = API.GetPedFaceFeature(ped, i);
            }
        }

        public int Father
        {
            get { return _shapeFirstID; }
        }

        public int Mother
        {
            get { return _shapeSecondID; }
        }

        public void Restore(int ped)
        {
            API.SetPedHeadBlendData(ped,
                _shapeFirstID, _shapeSecondID, _shapeThirdID,
                _skinFirstID, _skinSecondID, _skinThirdID,
                _shapeMix, _skinMix, _thirdMix,
                _isParent);

            API.SetPedHairColor(ped, _hairColor, _hairHighlightColor);

            for (int i = 0; i < HeadOverlaysCount; ++i)
            {
                var headOverlay = _headOverlays[i];
                API.SetPedHeadOverlay(ped, i, headOverlay.Style, headOverlay.Opacity);
                API.SetPedHeadOverlayColor(ped, i, headOverlay.ColorType, headOverlay.FirstColor, headOverlay.SecondColor);
            }

            for (int i = 0; i < FaceFeaturesCount; ++i)
            {
                API.SetPedFaceFeature(ped, i, _faceFeatures[i]);
            }
        }

        public override bool Equals(object value)
        {
            var other = value as MultiplayerSkin;
            return other != null &&
                _hairColor == other._hairColor &&
                _hairHighlightColor == other._hairHighlightColor &&
                _shapeFirstID == other._shapeFirstID &&
                _shapeSecondID == other._shapeSecondID &&
                _shapeThirdID == other._shapeThirdID &&
                _skinFirstID == other._skinFirstID &&
                _skinSecondID == other._skinSecondID &&
                _skinThirdID == other._skinThirdID &&
                _shapeMix == other._shapeMix &&
                _skinMix == other._skinMix &&
                _thirdMix == other._thirdMix &&
                _isParent == other._isParent &&
                Enumerable.SequenceEqual(_headOverlays, other._headOverlays) &&
                Enumerable.SequenceEqual(_faceFeatures, other._faceFeatures);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace PocceMod.Client
{
    public class MultiplayerSkin
    {
        private const int HeadOverlaysCount = 12;
        private const int FaceFeaturesCount = 20;

        private int _hairColor;
        private int _hairHighlightColor;
        private int _shapeFirstID;
        private int _shapeSecondID;
        private int _shapeThirdID;
        private int _skinFirstID;
        private int _skinSecondID;
        private int _skinThirdID;
        private float _shapeMix;
        private float _skinMix;
        private float _thirdMix;
        private bool _isParent;
        private HeadOverlay[] _headOverlays;
        private float[] _faceFeatures;

        private MultiplayerSkin()
        {
        }

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

        public dynamic Serialize()
        {
            dynamic result = new ExpandoObject();
            result.hairColor           = _hairColor;
            result.hairHighlightColor  = _hairHighlightColor;
            result.shapeFirstID        = _shapeFirstID;
            result.shapeSecondID       = _shapeSecondID;
            result.shapeThirdID        = _shapeThirdID;
            result.skinFirstID         = _skinFirstID;
            result.skinSecondID        = _skinSecondID;
            result.skinThirdID         = _skinThirdID;
            result.shapeMix            = _shapeMix;
            result.skinMix             = _skinMix;
            result.thirdMix            = _thirdMix;
            result.isParent            = _isParent;
            result.headOverlays        = _headOverlays.Select(headOverlay => headOverlay.Serialize()).ToArray();
            result.faceFeatures        = _faceFeatures;
            return result;
        }

        public static MultiplayerSkin Deserialize(dynamic data)
        {
            return new MultiplayerSkin
            {
                _hairColor          = data.hairColor,
                _hairHighlightColor = data.hairHighlightColor,
                _shapeFirstID       = data.shapeFirstID,
                _shapeSecondID      = data.shapeSecondID,
                _shapeThirdID       = data.shapeThirdID,
                _skinFirstID        = data.skinFirstID,
                _skinSecondID       = data.skinSecondID,
                _skinThirdID        = data.skinThirdID,
                _shapeMix           = data.shapeMix,
                _skinMix            = data.skinMix,
                _thirdMix           = data.thirdMix,
                _isParent           = data.isParent,
                _headOverlays       = ((IEnumerable<dynamic>)data.headOverlays).Select<dynamic, HeadOverlay>(headOverlay => HeadOverlay.Deserialize(headOverlay)).ToArray(),
                _faceFeatures       = ((IEnumerable<object>)data.faceFeatures).Cast<float>().ToArray()
            };
        }

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

            public int Style { get; }
            public int ColorType { get; }
            public int FirstColor { get; }
            public int SecondColor { get; }
            public float Opacity { get; }

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

            public dynamic Serialize()
            {
                dynamic result = new ExpandoObject();
                result.Style = Style;
                result.ColorType = ColorType;
                result.FirstColor = FirstColor;
                result.SecondColor = SecondColor;
                result.Opacity = Opacity;
                return result;
            }

            public static HeadOverlay Deserialize(dynamic data)
            {
                return new HeadOverlay(data.Style, data.ColorType, data.FirstColor, data.SecondColor, data.Opacity);
            }
        }
    }
}

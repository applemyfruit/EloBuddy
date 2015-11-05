using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Xml;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace VolatileAIO.Organs.Brain
{
    class SkinManager : Heart
    {
        private static XmlDocument _infoXml;
        private static Model[] _models;
        private static int _originalSkinIndex;

        internal struct Model
        {
            public readonly string Name;
            public readonly ModelSkin[] Skins;

            public Model(string name, ModelSkin[] skins)
            {
                Name = name;
                Skins = skins;
            }

            public string[] GetSkinNames()
            {
                return Skins.Select(skin => skin.Name).ToArray();
            }
        }

        internal struct ModelSkin
        {
            public readonly string Name;
            public readonly int Index;

            public ModelSkin(string name, string index)
            {
                Name = name;
                Index = int.Parse(index);
            }
        }

        public static Model GetModelByIndex(int index)
        {
            return _models[index];
        }

        public static Model GetModelByName(string name)
        {
            return
                _models.FirstOrDefault(
                    model => string.Equals(model.Name, name, StringComparison.CurrentCultureIgnoreCase));
        }

        public static string[] ModelNames;

        public static void Initialize()
        {
            _hackMenu.AddGroupLabel("Volatile Skinchanger");
            _hackMenu.Add("models", new Slider("Model - ", 0, 0, 0)).OnValueChange += SkinManager_OnModelSliderChange;
            _hackMenu.Add("skins", new Slider("Skin - Classic", 0, 0, 0)).OnValueChange += SkinManager_OnSkinSliderChange;
            _hackMenu.Add("resetModel", new CheckBox("Reset Model", false)).OnValueChange += SkinManager_OnResetModel;
            _hackMenu.Add("resetSkin", new CheckBox("Reset Skin", false)).OnValueChange += SkinManager_OnResetSkin;
            _hackMenu.AddSeparator();

            using (var infoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VolatileAIO.Organs.Brain.Data.SkinInfo.xml"))
                if (infoStream != null)
                    using (var infoReader = new StreamReader(infoStream))
                    {
                        _infoXml = new XmlDocument();
                        _infoXml.LoadXml(infoReader.ReadToEnd());
                    }
            if (_infoXml.DocumentElement != null)
                _models =
                    _infoXml.DocumentElement.ChildNodes.Cast<XmlElement>()
                        .Select(
                            model =>
                                new Model(model.Attributes["name"].Value,
                                    model.ChildNodes.Cast<XmlElement>()
                                        .Select(
                                            skin =>
                                                new ModelSkin(skin.Attributes["name"].Value, skin.Attributes["index"].Value))
                                        .ToArray()))
                        .ToArray();
            ModelNames = _models.Select(model => model.Name).ToArray();

            _originalSkinIndex = Player.SkinId;
            _hackMenu["models"].Cast<Slider>().MaxValue = _models.Length-1;
            _hackMenu["models"].Cast<Slider>().CurrentValue = Array.IndexOf(ModelNames, Player.ChampionName);
        }

        private static void SkinManager_OnResetSkin(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            _hackMenu["skins"].Cast<Slider>().CurrentValue = _originalSkinIndex;
            if (_hackMenu["resetSkin"].Cast<CheckBox>().CurrentValue)
                _hackMenu["resetSkin"].Cast<CheckBox>().CurrentValue = false;
        }

        private static void SkinManager_OnResetModel(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            _hackMenu["models"].Cast<Slider>().CurrentValue = Array.IndexOf(ModelNames, Player.ChampionName);

            if (_hackMenu["resetModel"].Cast<CheckBox>().CurrentValue)
            _hackMenu["resetModel"].Cast<CheckBox>().CurrentValue = false;
        }

        private static void SkinManager_OnSkinSliderChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            var model = GetModelByIndex(_hackMenu["models"].Cast<Slider>().CurrentValue);
            var skin = model.Skins[_hackMenu["skins"].Cast<Slider>().CurrentValue];
            _hackMenu["skins"].Cast<Slider>().DisplayName = "Skin - " + skin.Name;
            EloBuddy.Player.SetSkinId(skin.Index);
        }

        private static void SkinManager_OnModelSliderChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            var model = GetModelByIndex(_hackMenu["models"].Cast<Slider>().CurrentValue);
            _hackMenu["models"].Cast<Slider>().DisplayName = "Model - " + model.Name;
            EloBuddy.Player.SetModel(model.Name);
            _hackMenu["skins"].Cast<Slider>().CurrentValue = 0;
            _hackMenu["skins"].Cast<Slider>().MaxValue = model.Skins.Length - 1;
        }
    }
}
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
            HackMenu.AddGroupLabel("Volatile Skinchanger");
            HackMenu.AddLabel("PSA: Changing your Model might in rare cases crash the game." + Environment.NewLine +"This does not apply to changing skin.");
            HackMenu.Add("models", new Slider("Model - ", 0, 0, 0)).OnValueChange += SkinManager_OnModelSliderChange;
            HackMenu.Add("skins", new Slider("Skin - Classic", 0, 0, 0)).OnValueChange += SkinManager_OnSkinSliderChange;
            HackMenu.Add("resetModel", new CheckBox("Reset Model", false)).OnValueChange += SkinManager_OnResetModel;
            HackMenu.Add("resetSkin", new CheckBox("Reset Skin", false)).OnValueChange += SkinManager_OnResetSkin;
            HackMenu.AddSeparator();

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
            HackMenu["models"].Cast<Slider>().MaxValue = _models.Length-1;
            HackMenu["models"].Cast<Slider>().CurrentValue = Array.IndexOf(ModelNames, Player.ChampionName);
        }

        private static void SkinManager_OnResetSkin(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            HackMenu["skins"].Cast<Slider>().CurrentValue = _originalSkinIndex;
            if (HackMenu["resetSkin"].Cast<CheckBox>().CurrentValue)
                HackMenu["resetSkin"].Cast<CheckBox>().CurrentValue = false;
        }

        private static void SkinManager_OnResetModel(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
        {
            HackMenu["models"].Cast<Slider>().CurrentValue = Array.IndexOf(ModelNames, Player.ChampionName);

            if (HackMenu["resetModel"].Cast<CheckBox>().CurrentValue)
            HackMenu["resetModel"].Cast<CheckBox>().CurrentValue = false;
        }

        private static void SkinManager_OnSkinSliderChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            var model = GetModelByIndex(HackMenu["models"].Cast<Slider>().CurrentValue);
            var skin = model.Skins[HackMenu["skins"].Cast<Slider>().CurrentValue];
            HackMenu["skins"].Cast<Slider>().DisplayName = "Skin - " + skin.Name;
            EloBuddy.Player.SetSkinId(skin.Index);
        }

        private static void SkinManager_OnModelSliderChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            var model = GetModelByIndex(HackMenu["models"].Cast<Slider>().CurrentValue);
            HackMenu["models"].Cast<Slider>().DisplayName = "Model - " + model.Name;
            EloBuddy.Player.SetModel(model.Name);
            HackMenu["skins"].Cast<Slider>().CurrentValue = 0;
            HackMenu["skins"].Cast<Slider>().MaxValue = model.Skins.Length - 1;
        }
    }
}
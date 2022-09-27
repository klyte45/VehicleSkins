using ColossalFramework;
using Kwytto.Data;
using Kwytto.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using VehicleSkins.Singleton;

namespace VehicleSkins.Data
{

    [XmlRoot("VSSkinSelectionData")]
    public class VSSkinSelectionData : DataExtensionBase<VSSkinSelectionData>
    {
        public override string SaveId => "K45_VS_VSSkinSelectionData";

        [XmlIgnore]
        public SimpleXmlDictionary<string, HashSet<string>> GeneralExcludedSkins { get; set; } = new SimpleXmlDictionary<string, HashSet<string>>();

        [Obsolete("SerializationOnly")]
        [XmlElement("GeneralExcludedSkins")]
        public SimpleXmlDictionary<string, List<string>> GeneralExcludedSkins_Data
        {
            get
            {
                var output = new SimpleXmlDictionary<string, List<string>>();
                foreach (var kv in GeneralExcludedSkins)
                {
                    output[kv.Key] = kv.Value.Select(x => x.IsNullOrWhiteSpace() ? SkinsSingleton.EXCLUDED_DEFAULT_SKIN_STRING : x).ToList();
                }
                return output;
            }
            set
            {
                GeneralExcludedSkins = new SimpleXmlDictionary<string, HashSet<string>>();
                if (!(value is null))
                {
                    foreach (var kv in value)
                    {
                        GeneralExcludedSkins[kv.Key] = new HashSet<string>(kv.Value.Select(x => x == SkinsSingleton.EXCLUDED_DEFAULT_SKIN_STRING ? null : x));
                    }
                }
            }

        }
    }

}

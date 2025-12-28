
namespace MonitoringSystem.Utilities.Common
{
    public enum Month
    {
        January = 1,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }
    public class CommonEnumDisplay
    {
        private static string SplitEnumName(string enumName)
            {
            var name = string.Empty;
            var enumValues = enumName.Split('_');
            if(enumValues.Length > 1)
            {
                foreach(var a in enumValues)
                {
                    if(!string.IsNullOrWhiteSpace(name))
                    {
                        name = name + " " + a;
                    }
                    else
                    {
                        name = a;
                    }
                }
            }
            else
            {
                name = enumName;
            }
            return name;
        }

        public static List<EnumValueClass> GetEnumDisplayList(string EnumType)
        {
            List<EnumValueClass> modelList = new List<EnumValueClass>();
            EnumValueClass model;
            switch(EnumType)
            {
                case "ModuleType":
                    modelList = Enum.GetValues(typeof(ModuleType)).Cast<ModuleType>().Select(t => new EnumValueClass
                    {
                        Id = (int)t,
                        Name = SplitEnumName(t.ToString())
                    }).ToList();
                    break;
                
            }
            

            return modelList;
        }

        public static string GetEnumName(string EnumType, int enumValue)
        {
            string enumName = string.Empty;

            switch(EnumType)
            {
                case "ModuleType":
                    enumName = Enum.GetValues(typeof(ModuleType)).Cast<ModuleType>().Select(t => new EnumValueClass
                    {
                        Id = (int)t,
                        Name = SplitEnumName(t.ToString())
                    }).Where(x => x.Id == enumValue).FirstOrDefault().Name;
                    break;
                
			}
            
            return enumName; 
        }
		
		public static EnumValueClass GetEnumDisplayByName(string EnumType, int enumValue)
        {
            EnumValueClass result = new EnumValueClass();

            switch(EnumType)
            {
     //           case "OrStatusType":
     //               switch(enumValue)
     //               {
     //                   case(int)OrStatusType.Final:
     //                       result.Name = GetEnumName(EnumType, enumValue);
     //                       result.Id =(int)OrStatusType.Final;
     //                       result.ColorClass = "text-success";
					//		break;
					//	case(int)OrStatusType.Draft:
					//		result.Name = GetEnumName(EnumType, enumValue);
					//		result.Id =(int)OrStatusType.Draft;
					//		result.ColorClass = "text-warning";
					//		break;
					//}
     //               break;
            }

            return result;

		}
    }
}

using lumine8_GrpcService;

namespace lumine8
{
    public class EnumToString
    {
        public string ToSecurity(lumine8_GrpcService.Security security)
        {
            switch (security)
            {
                case lumine8_GrpcService.Security.PublicSecurity:
                    return "Public";
                case lumine8_GrpcService.Security.PrivateSecurity:
                    return "Private";
                default:
                    return security.ToString();
            }
        }

        public string ToRoleType(RoleType role)
        {
            switch (role)
            {
                case RoleType.NoRole:
                    return "No Role";
                default:
                    return role.ToString();
            }
        }

        public string ToStatus(Status status)
        {
            switch (status)
            {
                case Status.NoAnswer:
                    return "No Answer";
                case Status.ItsComplicated:
                    return "It's Complicated";
                default:
                    return status.ToString();
            }
        }

        public string ToLevel(Level level)
        {
            switch (level)
            {
                case Level.NoGraduation:
                    return "No Graduation";
                case Level.HighSchool:
                    return "High School";
                case Level.SomeCollege:
                    return "Some College";
                case Level.CollegeCreditCertificate:
                    return "College Credit Certificate";
                case Level.AssociateDegree:
                    return "Associate's Degrees";
                case Level.BachelorsDegree:
                    return "Bachelor's Degree";
                case Level.MastersDegree:
                    return "Master's Degree";
                case Level.DoctoralDegree:
                    return "Doctoral Degree";
                default:
                    return level.ToString();
            }
        }

        public string ToSecurityLevel(SecurityLevel security)
        {
            switch (security)
            {
                case SecurityLevel.PrivateLevel:
                    return "Private";
                case SecurityLevel.FriendsLevel:
                    return "Friends";
                case SecurityLevel.PublicLevel:
                    return "Public";
                default:
                    return security.ToString();
            }
        }

        public string ToRelationship(RelationshipType relationship)
        {
            switch (relationship)
            {
                case RelationshipType.FatherInLaw:
                    return "Father-In-Law";
                case RelationshipType.MotherInLaw:
                    return "Mother-In-Law";
                case RelationshipType.SonInLaw:
                    return "Son-In-Law";
                case RelationshipType.DaughterInLaw:
                    return "Daughter-In-Law";
                case RelationshipType.BrotherInLaw:
                    return "Brother-In-Law";
                case RelationshipType.SisterInLaw:
                    return "Sister-In-Law";
                case RelationshipType.HalfBrother:
                    return "Half Brother";
                case RelationshipType.HalfSister:
                    return "Half Sister";
                default:
                    return relationship.ToString();
            }
        }

        public string ToTagOptions(TagOptions options)
        {
            switch (options)
            {
                case TagOptions.NoFilter:
                    return "No Filter";
                case TagOptions.FilterChecked:
                    return "Filter Checked";
                case TagOptions.FilterBoth:
                    return "Filter Both";
                default:
                    return options.ToString();
            }
        }
    }
}

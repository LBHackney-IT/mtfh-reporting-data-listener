using System;
using MMH;
using TenureDomain = Hackney.Shared.Tenure.Domain;
using Hackney.Shared.Tenure.Boundary.Response;
using System.Linq;

namespace MtfhReportingDataListener.Domain
{
    public static class MappingExtensions
    {
        public static int? ToAvroDate(this DateTime? date)
        {
            return (int?) (date?.Subtract(new DateTime(1970, 1, 1)))?.TotalDays;
        }
        public static int ToAvroDate(this DateTime date)
        {
            return (int) (date.Subtract(new DateTime(1970, 1, 1))).TotalDays;
        }

        public static HouseholdMembersType ToAvro(this TenureDomain.HouseholdMembersType memberTypeEnum) => memberTypeEnum switch
        {
            Hackney.Shared.Tenure.Domain.HouseholdMembersType.Person => MMH.HouseholdMembersType.Person,
            Hackney.Shared.Tenure.Domain.HouseholdMembersType.Organisation => MMH.HouseholdMembersType.Organization,
            _ => throw new ArgumentOutOfRangeException(nameof(memberTypeEnum), $"Unexpected HouseholdMemberType enum value for value: {memberTypeEnum}")
        };
        public static PersonTenureType ToAvro(this TenureDomain.PersonTenureType personTenureType) => personTenureType switch
        {
            Hackney.Shared.Tenure.Domain.PersonTenureType.Tenant => MMH.PersonTenureType.Tenant,
            Hackney.Shared.Tenure.Domain.PersonTenureType.Leaseholder => MMH.PersonTenureType.Leaseholder,
            Hackney.Shared.Tenure.Domain.PersonTenureType.Freeholder => MMH.PersonTenureType.Freeholder,
            Hackney.Shared.Tenure.Domain.PersonTenureType.HouseholdMember => MMH.PersonTenureType.HouseholdMember,
            Hackney.Shared.Tenure.Domain.PersonTenureType.Occupant => MMH.PersonTenureType.Occupant,
            _ => throw new ArgumentOutOfRangeException(nameof(personTenureType), $"Unexpected PersonTenureType enum value for value: {personTenureType}")
        };

        public static HouseholdMember ToAvro(this TenureDomain.HouseholdMembers householdMember)
        {
            return new HouseholdMember
            {
                Id = householdMember.Id.ToString(),
                HouseholdMembersType = householdMember.Type.ToAvro(),
                FullName = householdMember.FullName,
                IsResponsible = householdMember.IsResponsible,
                DateOfBirth = householdMember.DateOfBirth.ToAvroDate(),
                PersonTenureType = householdMember.PersonTenureType.ToAvro()
            };
        }

        public static MMH.TenuredAssetType ToAvro(this TenureDomain.TenuredAssetType tenuredAssetType) => tenuredAssetType switch
        {
            Hackney.Shared.Tenure.Domain.TenuredAssetType.Block => MMH.TenuredAssetType.Block,
            Hackney.Shared.Tenure.Domain.TenuredAssetType.Concierge => MMH.TenuredAssetType.Concierge,
            Hackney.Shared.Tenure.Domain.TenuredAssetType.Dwelling => MMH.TenuredAssetType.Dwelling,
            Hackney.Shared.Tenure.Domain.TenuredAssetType.LettableNonDwelling => MMH.TenuredAssetType.LettableNonDwelling,
            Hackney.Shared.Tenure.Domain.TenuredAssetType.MediumRiseBlock => MMH.TenuredAssetType.MediumRiseBlock,
            Hackney.Shared.Tenure.Domain.TenuredAssetType.NA => MMH.TenuredAssetType.NA,
            Hackney.Shared.Tenure.Domain.TenuredAssetType.TravellerSite => MMH.TenuredAssetType.TravellerSite,
            _ => throw new ArgumentOutOfRangeException(nameof(tenuredAssetType), $"Unexpected TenuredTenureType enum value for value: {tenuredAssetType}")
        };

        public static MMH.TenuredAsset ToAvro(this TenureDomain.TenuredAsset tenuredAsset)
        {
            return new MMH.TenuredAsset
            {
                Id = tenuredAsset.Id.ToString(),
                TenuredAssetType = tenuredAsset.Type?.ToAvro(),
                FullAddress = tenuredAsset.FullAddress,
                Uprn = tenuredAsset.Uprn,
                PropertyReference = tenuredAsset.PropertyReference
            };
        }

        public static MMH.Charges ToAvro(this TenureDomain.Charges charges)
        {
            return new MMH.Charges
            {
                Rent = charges.Rent,
                CurrentBalance = charges.CurrentBalance,
                BillingFrequency = charges.BillingFrequency,
                ServiceCharge = charges.ServiceCharge,
                OtherCharges = charges.OtherCharges,
                CombinedRentCharges = charges.CombinedRentCharges,
                CombinedServiceCharges = charges.CombinedServiceCharges,
                TenancyInsuranceCharge = charges.TenancyInsuranceCharge,
                OriginalRentCharge = charges.OriginalRentCharge,
                OriginalServiceCharge = charges.OriginalServiceCharge
            };
        }

        public static MMH.TenureType ToAvro(this TenureDomain.TenureType tenureType)
        {
            return new MMH.TenureType
            {
                Code = tenureType.Code,
                Description = tenureType.Description
            };
        }

        public static MMH.AgreementType ToAvro(this TenureDomain.AgreementType agreementType)
        {
            return new MMH.AgreementType
            {
                Code = agreementType.Code,
                Description = agreementType.Description
            };
        }

        public static MMH.Terminated ToAvro(this TenureDomain.Terminated terminated)
        {
            return new MMH.Terminated
            {
                IsTerminated = terminated.IsTerminated,
                ReasonForTermination = terminated.ReasonForTermination
            };
        }

        public static MMH.Notices ToAvro(this TenureDomain.Notices notices)
        {
            return new MMH.Notices
            {
                Type = notices.Type,
                ServedDate = notices.ServedDate.ToAvroDate(),
                ExpiryDate = notices.ExpiryDate.ToAvroDate(),
                EffectiveDate = notices.EffectiveDate.ToAvroDate(),
                EndDate = notices.EndDate.ToAvroDate()
            };
        }

        public static MMH.LegacyReferences ToAvro(this TenureDomain.LegacyReference references)
        {
            return new MMH.LegacyReferences
            {
                Name = references.Name,
                Value = references.Value
            };
        }

        public static MMH.TenureInformation ToAvro(this TenureResponseObject tenure)
        {

            return new MMH.TenureInformation
            {
                Id = tenure.Id.ToString(),
                PaymentReference = tenure.PaymentReference,
                StartOfTenureDate = tenure.StartOfTenureDate.ToAvroDate(),
                EndOfTenureDate = tenure.EndOfTenureDate.ToAvroDate(),
                IsTenanted = tenure.IsTenanted,
                SuccessionDate = tenure.SuccessionDate.ToAvroDate(),
                EvictionDate = tenure.EvictionDate.ToAvroDate(),
                PotentialEndDate = tenure.PotentialEndDate.ToAvroDate(),
                IsMutualExchange = tenure.IsMutualExchange,
                InformHousingBenefitsForChanges = tenure.InformHousingBenefitsForChanges,
                IsSublet = tenure.IsSublet,
                SubletEndDate = tenure.SubletEndDate.ToAvroDate(),
                HouseholdMembers = tenure.HouseholdMembers.Select(member => member.ToAvro()).ToList(),
                TenuredAsset = tenure.TenuredAsset.ToAvro(),
                Charges = tenure.Charges.ToAvro(),
                TenureType = tenure.TenureType.ToAvro(),
                Terminated = tenure.Terminated.ToAvro(),
                AgreementType = tenure.AgreementType.ToAvro(),
                Notices = tenure.Notices.Select(notice => notice.ToAvro()).ToList(),
                LegacyReferences = tenure.LegacyReferences.Select(reference => reference.ToAvro()).ToList(),
                VersionNumber = 1
            };
        }
    }
}

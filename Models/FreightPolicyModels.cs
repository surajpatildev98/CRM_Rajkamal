namespace CRMWebServices.Models
{
    public class FreightPolicyModels
    {
        public class CreateFreightPolicyRequest
        {
            public string PolicyName { get; set; }
            public List<FreightPolicySlabRequest> Slabs { get; set; }
        }

        public class FreightPolicySlabRequest
        {
            public decimal MinOrderValue { get; set; }
            public decimal? MaxOrderValue { get; set; }
            public decimal FreightAmount { get; set; }
        }

        public class FreightPolicyListResponse
        {
            public int FreightPolicyId { get; set; }
            public string PolicyName { get; set; }
            public int VersionNo { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedOn { get; set; }
            public long? CreatedBy { get; set; }
            public int SlabCount { get; set; }
        }

        public class FreightPolicyDetailsResponse
        {
            public int FreightPolicyId { get; set; }
            public string PolicyName { get; set; }
            public int VersionNo { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedOn { get; set; }
            public long? CreatedBy { get; set; }
            public DateTime? UpdatedOn { get; set; }
            public long? UpdatedBy { get; set; }
            public List<FreightPolicySlabResponse> Slabs { get; set; }
        }

        public class FreightPolicySlabResponse
        {
            public int FreightPolicySlabId { get; set; }
            public int FreightPolicyId { get; set; }
            public decimal MinOrderValue { get; set; }
            public decimal? MaxOrderValue { get; set; }
            public decimal FreightAmount { get; set; }
        }

        public class ApplicableFreightResponse
        {
            public int FreightPolicyId { get; set; }
            public string PolicyName { get; set; }
            public int VersionNo { get; set; }
            public int FreightPolicySlabId { get; set; }
            public decimal MinOrderValue { get; set; }
            public decimal? MaxOrderValue { get; set; }
            public decimal FreightAmount { get; set; }
        }
        public class UpdateFreightPolicyRequest
        {
            public int FreightPolicyId { get; set; }
            public List<FreightPolicySlabRequest> Slabs { get; set; } = new();
        }

        public class UpdateFreightPolicyResponse
        {
            public int FreightPolicyId { get; set; }
            public string PolicyName { get; set; } = string.Empty;
            public int VersionNo { get; set; }
            public bool IsActive { get; set; }
            public List<FreightPolicySlabResponse> Slabs { get; set; } = new();
        }

        public class DeactivateFreightPolicyRequest
        {
            public int FreightPolicyId { get; set; }
        }

    }
}
    
﻿namespace AonFreelancing.Models.DTOs
{
    public class BidOutDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }

        public ProjectOutDTO Project { get; set; }
        public long FreelancerId { get; set; }
        public FreelancerShortOutDTO Freelancer { get; set; }
        public decimal ProposedPrice { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
        public DateTime? ApprovedAt { get; set; }
    }
}

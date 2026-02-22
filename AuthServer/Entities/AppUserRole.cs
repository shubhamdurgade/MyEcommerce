namespace AuthServer.Entities
{
    public class AppUserRole
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; } = default!;

        public AppUser User { get; set; } = default!;

        public Guid RoleId { get; set; } = default!;

        public AppRole Role { get; set; } = default!;

        public Guid? AssignedByUserId { get; set; }

        public DateTime AssignedUtc { get; set; }

        public string? Notes { get; set; }
    }
}

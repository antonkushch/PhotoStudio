using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace PhotoStudioDiploma.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Surname { get; set; }

        public virtual PhotographerInfo PhotographerInfo { get; set; }
        public virtual ClientInfo ClientInfo { get; set; }

        public virtual ICollection<PhotographerFolder> PhotographerFolders { get; set; }

        // Dropbox folders that this user has access to
        public virtual ICollection<PhotographerFolder> GrantedFolders { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class PhotographerInfo
    {
        [Key, ForeignKey("ApplicationUser")]
        public string ApplicationUserId { get; set; }
        public string DropboxAccessToken { get; set; }
        public string ConnectState { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }
    }

    public class ClientInfo
    {
        [Key, ForeignKey("ApplicationUser")]
        public string ApplicationUserId { get; set; }
        public int ClientInteger { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }
    }

    public class PhotographerFolder
    {
        public int PhotographerFolderId { get; set; }
        public string Name { get; set; }
        public int Depth { get; set; }
        public string Path { get; set; }
        public string DropboxCursor { get; set; }
        public string DropboxFolderId { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public virtual ICollection<PhotographerFile> PhotographerFiles { get; set; }

        // Clients who have access to this dropbox folder
        public virtual ICollection<ApplicationUser> GrantedUsers { get; set; }
    }

    public class PhotographerFile
    {
        public int PhotographerFileId { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public int Depth { get; set; }
        public string Path { get; set; }
        public byte[] ThumbnailImage { get; set; }
        public string DropboxFileId { get; set; }

        public int PhotographerFolderId { get; set; }
        public virtual PhotographerFolder PhotographerFolder { get; set; }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<PhotographerFolder> PhotographerFolders { get; set; }
        public DbSet<PhotographerFile> PhotographerFiles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>()
                .HasMany<PhotographerFolder>(u => u.GrantedFolders)
                .WithMany(f => f.GrantedUsers)
                .Map(uf =>
                {
                    uf.MapLeftKey("ApplicationUserRefId");
                    uf.MapRightKey("PhotographerFolderRefId");
                    uf.ToTable("GrantedUserFolders");
                });

            modelBuilder.Entity<ClientInfo>()
                .HasRequired(a => a.ApplicationUser)
                .WithOptional(u => u.ClientInfo)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<PhotographerInfo>()
                .HasRequired(b => b.ApplicationUser)
                .WithOptional(d => d.PhotographerInfo)
                .WillCascadeOnDelete(true);

            base.OnModelCreating(modelBuilder);
        }
    }
}
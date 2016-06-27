using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Draftkings.Ownership.Models
{
    public class ContestGroup
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int ContestGroupId { get; set; }
        public int SourceId { get; set; }
        public int SportId { get; set; }
        public string ContestGroupName { get; set; }
        public int DraftGroupId { get; set; }
        public DateTime ContestStartDate { get; set; }
        public DateTime LastGameStart { get; set; }
        public int GameCount { get; set; }
        public bool ActiveFlag { get; set; }
        public int ContestSortOrder { get; set; }
    }
    public class Contest
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int ContestId { get; set; }
        public int ContestGroupId { get; set; }
        public int DraftGroupId { get; set; }
        public string ContestTitle { get; set; }
        public string ContestType { get; set; }
        public int EntryCount { get; set; }
        public int MultiEntry { get; set; }
        public bool ActiveFlag { get; set; }
        public float Rake { get; set; }
        public float EntryFee { get; set; }
        public int Size { get; set; }
        public bool UserCreated { get; set; }
        
    }
    public class ContestScrapeStatus
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int ContestId { get; set; }
        public int ContestGroupId { get; set; }
        public bool InitialEntryIdScrape { get; set; }
        public bool FinalEntryIdScrape { get; set; }
        public bool IsOwnershipCollected { get; set; }
    }
    public class Prizes
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int ContestId { get; set; }
        public int WinnerCount { get; set; }
        public double TotalPrizes { get; set; }
        public string Summary { get; set; }
    }
    public class ContestEntry
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 0)]
        public int EntryId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 1)]
        public int ContestId { get; set; }
        public int Rank { get; set; }
        public float Score { get; set; }
        public bool IsWinner { get; set; }
        public string Username { get; set; }
    }
    public class ContestPlayer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 0)]
        public int ContestId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 1)]
        public int PlayerId { get; set; }
        public float Ownership { get; set; }
    }
    public class DraftGroupPlayer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 0)]
        public int DraftGroupId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key, Column(Order = 1)]
        public int PlayerId { get; set; }
        public int Salary { get; set; }
    }
    public class LoginInfo
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime Created { get; set; }
    }
    public class FantasyContestsDBContextDk : DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // other code 
            Database.SetInitializer<FantasyContestsDBContextDk>(null);
            // more code
        }
        //public FantasyContestsDBContextDk()
        //    : base("FantasyContestsDBContextDk")
        //{ }
        public DbSet<ContestGroup> ContestGroups { get; set; }
        public DbSet<Contest> Contests { get; set; }
        public DbSet<Prizes> Prize { get; set; }
        public DbSet<ContestEntry> ContestEntries { get; set; }
        public DbSet<ContestPlayer> ContestPlayers { get; set; }
        public DbSet<DraftGroupPlayer> DraftGroupPlayers { get; set; }
        public DbSet<ContestScrapeStatus> ScrapeStatuses { get; set; }
        public DbSet<LoginInfo> Logins { get; set; }
    }
}
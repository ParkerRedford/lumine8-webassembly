using Google.Protobuf.WellKnownTypes;
using lumine8_GrpcService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System.ComponentModel.DataAnnotations.Schema;
using Exception = lumine8_GrpcService.Exception;
using Image = lumine8_GrpcService.Image;
using Share = lumine8_GrpcService.Share;

namespace lumine8.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //This setup is for PostgreSQL. Change for a different relational database. The models were generated from main.proto
            //You can use ApplicationDbContextModelSnapshot.cs under the Migrations folder to create your database: update-database -context applicationdbcontext

            var converter = new ValueConverter<Timestamp, DateTime>(v => v.ToDateTime().ToUniversalTime(), v => Timestamp.FromDateTime(v.ToUniversalTime()));
            //builder.Entity<Timestamp>().HasNoKey();
            builder.Ignore<Timestamp>();

            //Users
            builder.Entity<ApplicationUser>().HasKey(k => k.Id);
            builder.Entity<ApplicationUser>().HasAlternateKey(k => k.Username);
            builder.Entity<ApplicationUser>().Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
            builder.Entity<ApplicationUser>().Property(p => p.UserSince).IsRequired().HasConversion(converter);

            builder.Entity<Token>().HasKey(k => k.TokenId);
            builder.Entity<Token>().Property(p => p.TokenId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Request>().HasKey(k => k.RequestId);
            builder.Entity<Request>().Property(p => p.RequestId).HasDefaultValueSql("gen_random_uuid()");

            //Friends
            builder.Entity<FriendDuo>().HasKey(k => k.FriendDuoId);
            builder.Entity<FriendDuo>().Property(p => p.FriendDuoId).HasDefaultValueSql("gen_random_uuid()");

            //Groups
            builder.Entity<GroupModel>().HasKey(k => k.GroupId);
            builder.Entity<GroupModel>().Property(p => p.CreateDate).IsRequired().HasConversion(converter);
            builder.Entity<GroupModel>().Property(p => p.GroupId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<GroupApproval>().HasKey(k => k.GroupApprovalId);
            builder.Entity<GroupApproval>().Property(p => p.GroupApprovalId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<GroupRequest>().HasKey(k => k.RequestId);
            builder.Entity<GroupRequest>().Property(p => p.RequestId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<SectionRoles>().HasKey(k => k.SectionRolesId);
            builder.Entity<SectionRoles>().Property(p => p.SectionRolesId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<GroupImage>().HasKey(k => k.ImageId);
            builder.Entity<GroupImage>().Property(p => p.CreateDate).IsRequired().HasConversion(converter);
            builder.Entity<GroupImage>().Property(p => p.ImageId).HasDefaultValueSql("gen_random_uuid()");


            builder.Entity<LumineCheck>().HasKey(k => k.CheckId);
            builder.Entity<LumineCheck>().Property(p => p.CheckId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<GroupUserComment>().HasKey(k => k.CommentId);
            builder.Entity<GroupUserComment>().Property(p => p.CreateDate).HasConversion(converter);
            builder.Entity<GroupUserComment>().Property(p => p.CommentId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Hashtag>().HasKey(k => k.HashtagId);
            builder.Entity<Hashtag>().Property(p => p.HashtagId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Ban>().HasKey(k => k.BanId);
            builder.Entity<Ban>().Property(p => p.BanId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Exception>().HasKey(k => k.ExceptionId);
            builder.Entity<Exception>().Property(p => p.ExceptionId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Role>().HasKey(k => k.RoleId);
            builder.Entity<Role>().Property(p => p.RoleId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<GroupLink>().HasKey(k => k.LinkId);
            builder.Entity<GroupLink>().Property(p => p.LinkId).HasDefaultValueSql("gen_random_uuid()");

            //Images
            builder.Entity<Image>().HasKey(k => k.ImageId);
            builder.Entity<Image>().Property(p => p.UploadDate).IsRequired().HasConversion(converter);
            builder.Entity<Image>().Property(p => p.ImageId).HasDefaultValueSql("gen_random_uuid()");


            builder.Entity<UserComment>().HasKey(k => k.UserCommentId);
            builder.Entity<UserComment>().Property(p => p.Date).IsRequired().HasConversion(converter);
            builder.Entity<UserComment>().Property(p => p.UserCommentId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<ImageRoom>().HasKey(k => k.ImageRoomId);
            builder.Entity<ImageRoom>().Property(p => p.ImageRoomId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<CategoryRooms>().HasKey(k => k.CategoryRoomsId);
            builder.Entity<CategoryRooms>().Property(p => p.CategoryRoomsId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<ProfilePicture>().HasKey(k => k.ProfilePictureId);
            builder.Entity<ProfilePicture>().Property(p => p.ProfilePictureId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<GroupProfilePicture>().HasKey(k => k.ProfilePictureId);
            builder.Entity<GroupProfilePicture>().Property(p => p.ProfilePictureId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<MainPicture>().HasKey(k => k.UserId);
            builder.Entity<MainPicture>().Property(p => p.UserId).HasDefaultValueSql("gen_random_uuid()");

            //Messages
            builder.Entity<Message>().HasKey(k => k.MessageId);
            builder.Entity<Message>().Property(p => p.Date).HasConversion(converter);
            builder.Entity<Message>().Property(p => p.MessageId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Room>().HasKey(k => k.RoomId);
            builder.Entity<Room>().Property(p => p.Date).IsRequired().HasConversion(converter);
            builder.Entity<Room>().Property(p => p.RoomId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<RoomGroup>().HasKey(k => k.RoomGroupId);
            builder.Entity<RoomGroup>().Property(p => p.RoomGroupId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Vote>().HasKey(k => k.VoteId);
            builder.Entity<Vote>().Property(p => p.Date).IsRequired().HasConversion(converter);
            builder.Entity<Vote>().Property(p => p.VoteId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Share>().HasKey(k => k.ShareId);
            builder.Entity<Share>().Property(p => p.Date).IsRequired().HasConversion(converter);
            builder.Entity<Share>().Property(p => p.ShareId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<MessageOnMessage>().HasKey(k => k.MessageOnMessageId);
            builder.Entity<MessageOnMessage>().Property(p => p.MessageOnMessageId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<RoomToMessage>().HasKey(k => k.RoomToMessageId);
            builder.Entity<RoomToMessage>().Property(p => p.RoomToMessageId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<MessageHashtag>().HasKey(k => k.MessageHashtagId);
            builder.Entity<MessageHashtag>().Property(p => p.MessageHashtagId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Like>().HasKey(k => k.LikeId);
            builder.Entity<Like>().Property(p => p.LikeId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<ChatMessage>().HasKey(k => k.MessageId);
            builder.Entity<ChatMessage>().Property(p => p.Date).IsRequired().HasConversion(converter);
            builder.Entity<ChatMessage>().Property(p => p.MessageId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<UserRoom>().HasKey(k => k.UserRoomId);
            builder.Entity<UserRoom>().Property(p => p.UserRoomId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Notification>().HasKey(k => k.NotificationId);
            builder.Entity<Notification>().Property(p => p.NotificationId).HasDefaultValueSql("gen_random_uuid()");

            //Personal
            builder.Entity<Lived>().HasKey(k => k.PlaceLivedId);
            builder.Entity<Lived>().Property(p => p.From).IsRequired().HasConversion(converter);
            builder.Entity<Lived>().Property(p => p.To).IsRequired().HasConversion(converter);
            builder.Entity<Lived>().Property(p => p.PlaceLivedId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<WorkHistory>().HasKey(k => k.WorkId);
            builder.Entity<WorkHistory>().Property(p => p.From).IsRequired().HasConversion(converter);
            builder.Entity<WorkHistory>().Property(p => p.To).IsRequired().HasConversion(converter);
            builder.Entity<WorkHistory>().Property(p => p.WorkId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Education>().HasKey(k => k.EducationId);
            builder.Entity<Education>().Property(p => p.EducationId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<About>().HasKey(k => k.AboutId);
            builder.Entity<About>().Property(p => p.DOB).IsRequired().HasConversion(converter);

            builder.Entity<AboutMe>().HasKey(k => k.UserId);

            builder.Entity<Link>().HasKey(k => k.LinkId);
            builder.Entity<Link>().Property(p => p.LinkId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<Interest>().HasKey(k => k.InterestId);
            builder.Entity<Interest>().Property(p => p.InterestId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<ProfileSecurity>().HasKey(k => k.UserId);

            builder.Entity<PrivateProfile>().HasKey(k => k.PrivateProfileId);
            builder.Entity<PrivateProfile>().Property(p => p.PrivateProfileId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<TagsFeed>().HasKey(k => k.TagsFeedId);
            builder.Entity<TagsFeed>().Property(p => p.TagsFeedId).HasDefaultValueSql("gen_random_uuid()");

            //Location
            builder.Entity<Country>().HasKey(k => k.Id);
            builder.Entity<State>().HasKey(k => k.Id);
            //builder.Entity<State>().HasAlternateKey(k => k.CountryId);
            builder.Entity<City>().HasKey(k => k.Id);
            //builder.Entity<City>().HasAlternateKey(k => k.StateId);

            //Video
            builder.Entity<Video>().HasKey(k => k.VideoId);
            builder.Entity<Video>().Property(p => p.UploadDate).IsRequired().HasConversion(converter);
            builder.Entity<Video>().Property(p => p.VideoId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<VideoLike>().HasKey(k => k.VideoLikeId);
            builder.Entity<VideoLike>().Property(p => p.LikeDate).IsRequired().HasConversion(converter);
            builder.Entity<VideoLike>().Property(p => p.VideoLikeId).HasDefaultValue("gen_random_uuid()");

            builder.Entity<Subscribe>().HasKey(k => k.SubscribeId);
            builder.Entity<Subscribe>().Property(p => p.SubscribeId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<VideoComment>().HasKey(k => k.CommentId);
            builder.Entity<VideoComment>().Property(p => p.Date).IsRequired().HasConversion(converter);
            builder.Entity<VideoComment>().Property(p => p.CommentId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<VideoCommentOn>().HasKey(k => k.Id);
            builder.Entity<VideoCommentOn>().Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<VideoRoom>().HasKey(k => k.VideoRoomId);
            builder.Entity<VideoRoom>().Property(p => p.VideoRoomId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<FAQ>().HasKey(k => k.FAQId);
            builder.Entity<FAQ>().Property(p => p.FAQId).HasDefaultValueSql("gen_random_uuid()");

            builder.Entity<PFAQ>().HasKey(k => k.FAQId);
            builder.Entity<PFAQ>().Property(p => p.FAQId).HasDefaultValueSql("gen_random_uuid()");

            //Petiions
            builder.Entity<PetitionModel>().HasKey(k => k.PetitionId);
            builder.Entity<PetitionModel>().Property(p => p.PetitionId).HasDefaultValue("gen_random_uuid()");

            builder.Entity<PetitionSig>().HasKey(k => k.PetitionSigId);
            builder.Entity<PetitionSig>().Property(p => p.PetitionSigId).HasDefaultValue("gen_random_uuid()");

            /*
            builder.Entity<ApplicationUser>().HasMany(x => x.FriendDuos).WithOne(e => e.ApplicationUser).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.FriendDuos).WithOne(e => e.ApplicationUser).HasForeignKey(x => x.FriendId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Requests).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.SenderId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Requests).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.SentToId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.GroupModels).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.OwnerId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.GroupApprovals).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.GroupRequests).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Bans).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Exceptions).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.GroupImages).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.GroupUserComments).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Roles).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Likes).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Messages).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.SenderId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Shares).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Shares).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.SenderId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.UserComment).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.About).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.AboutId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.AboutMe).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.EducationList).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.PFAQs).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Interests).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Links).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.WorkHistories).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Liveds).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.MainPicture).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.PrivateProfiles).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.PrivateProfiles).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.WhoId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.ProfilePictures).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.ProfileSecurity).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Relationships).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Subscribes).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Subscribes).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.SubscriberId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Videos).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.OwnerId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.VideoComments).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Rooms).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.OwnerId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Images).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Votes).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.TagsFeeds).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Notifications).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.UserRooms).WithOne(e => e.ApplicationUser).HasForeignKey(k =>k .UserId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.UserRooms).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.OtherId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.ChatMessages).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.SenderId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ApplicationUser>().HasMany(x => x.Petitions).WithOne(e => e.ApplicationUser).HasForeignKey(k => k.CreatedById).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupModel>().HasMany(x => x.Bans).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.Exceptions).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.FAQs).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.GroupApprovals).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.GroupImages).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.GroupLinks).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.GroupProfilePictures).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.GroupRequests).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.Hashtags).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.LumineChecks).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.Roles).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.SectionRoles).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<GroupModel>().HasMany(x => x.RoomGroups).WithOne(e => e.GroupModel).HasForeignKey(k => k.GroupId).OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<GroupImage>().HasMany(x => x.GroupUserComments).WithOne(e => e.GroupImage).HasForeignKey(k => k.ImageId).OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<Image>().HasMany(x => x.UserComments).WithOne(e => e.Image).HasForeignKey(k => k.ImageId).OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<Message>().HasMany(k => k.Likes).WithOne(e => e.Message).HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Message>().HasMany(k => k.RoomToMessages).WithOne(e => e.Message).HasForeignKey(e => e.MessageId).OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<Room>().HasMany(k => k.Messages).WithOne(k => k.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.RoomToMessages).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.MessageHashtags).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.LumineChecks).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.VideoRooms).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.CategoryRooms).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.Votes).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.Shares).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.RoomGroups).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Room>().HasMany(k => k.ImageRooms).WithOne(e => e.Room).HasForeignKey(e => e.RoomId).OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<RoomToMessage>().HasMany(k => k.Shares).WithOne(e => e.RoomToMessage).HasForeignKey(k => k.RoomId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<RoomToMessage>().HasMany(k => k.CategoryRooms).WithOne(e => e.RoomToMessage).HasForeignKey(k => k.RoomId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Video>().HasMany(k => k.VideoComments).WithOne(e => e.Video).HasForeignKey(k => k.VideoId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Video>().HasMany(k => k.VideoRooms).WithOne(e => e.Video).HasForeignKey(k => k.VideoId).OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<VideoComment>().HasMany(k => k.VideoCommentOns).WithOne(e => e.VideoComment).HasForeignKey(k => k.CommentOnId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<VideoCommentOn>().HasMany(k => k.VideoComments).WithOne(e => e.VideoCommentOn).HasForeignKey(k => k.CommentId).OnDelete(DeleteBehavior.Cascade);
        
            builder.Entity<PetitionModel>().HasMany(k => k.PetitionSigs).WithOne(e => e.Petition).HasForeignKey(k => k.PetitionId).OnDelete(DeleteBehavior.Cascade);
        */
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        //Users
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<Token> Tokens { get; set; }

        //Friends
        public DbSet<Request> Requests { get; set; }
        public DbSet<FriendDuo> Friends { get; set; }

        //Groups
        public DbSet<GroupModel> Groups { get; set; }
        public DbSet<GroupApproval> GroupApprovals { get; set; }
        public DbSet<GroupRequest> GroupRequests { get; set; }
        public DbSet<SectionRoles> SectionRoles { get; set; }
        public DbSet<GroupImage> GroupImages { get; set; }
        public DbSet<LumineCheck> LumineChecks { get; set; }
        public DbSet<GroupUserComment> GroupUserComments { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<Exception> Exceptions { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<GroupLink> GroupLinks { get; set; }

        //Images
        public DbSet<Image> Images { get; set; }
        public DbSet<UserComment> UserComments { get; set; }
        public DbSet<ImageRoom> ImageRooms { get; set; }
        public DbSet<CategoryRooms> CategoryRooms { get; set; }
        public DbSet<ProfilePicture> ProfilePictures { get; set; }
        public DbSet<GroupProfilePicture> GroupProfilePictures { get; set; }
        public DbSet<MainPicture> MainPictures { get; set; }

        //Messages
        public DbSet<Message> Messages { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomGroup> RoomGroups { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<Share> Shares { get; set; }
        public DbSet<MessageOnMessage> MessageOnMessages { get; set; }
        public DbSet<RoomToMessage> RoomToMessages { get; set; }
        public DbSet<MessageHashtag> MessageHashtags { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<UserRoom> UserRooms { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        //Personal
        public DbSet<Lived> PlacesLived { get; set; }
        public DbSet<WorkHistory> WorkHistory { get; set; }
        public DbSet<Education> EducationList { get; set; }
        public DbSet<About> About { get; set; }
        public DbSet<AboutMe> AboutMe { get; set; }
        public DbSet<Link> Links { get; set; }
        public DbSet<Interest> Interests { get; set; }
        public DbSet<ProfileSecurity> ProfileSecurities { get; set; }
        public DbSet<PrivateProfile> PrivateProfiles { get; set; }
        public DbSet<TagsFeed> TagsFeeds { get; set; }

        //Location
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }

        //Video
        public DbSet<Video> Videos { get; set; }
        public DbSet<Subscribe> Subscribes { get; set; }
        public DbSet<VideoComment> VideoComments { get; set; }
        public DbSet<VideoCommentOn> VideoCommentOns { get; set; }
        public DbSet<VideoRoom> VideoRooms { get; set; }
        public DbSet<VideoLike> VideoLikes { get; set; }

        //Petitions
        public DbSet<PetitionModel> Petitions { get; set; }
        public DbSet<PetitionSig> PetitionSigs { get; set; }
    }
}

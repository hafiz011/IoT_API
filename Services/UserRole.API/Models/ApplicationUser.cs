using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace UserRoleAPI.Models
{
    public class ApplicationUser : MongoIdentityUser<Guid>
    {
        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("Phone")]
        public string Phone { get; set; }

        [BsonElement("Address")]
        public Address Address { get; set; }

        [BsonElement("ImagePath")]
        public string ImagePath { get; set; }

        [BsonElement("RefreshToken")]
        public string RefreshToken { get; set; }

        [BsonElement("RefreshTokenExpiryTime")]
        public DateTime RefreshTokenExpiryTime { get; set; }
    }

    public class Address
    {
        public string address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }

}

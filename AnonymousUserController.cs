using Fareportal.Crm.Libraries.CommonTypes;
using Fareportal.Crm.Libraries.Helpers;
using Fareportal.Crm.ViewModels;
using Fareportal.UserProfile.WebAPI.Utility;
using NLog;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Fareportal.Crm.Models.Anonymous;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
namespace Fareportal.UserProfile.WebAPI.Areas.PublicApi.Controllers.V1
{
    /// <summary>
    /// User for Anonymous user functionality
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AnonymousUserController : BaseController
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        bool IsEnableAnonymous;
        public AnonymousUserController() {
            this.AnonymousDBContext = new AnonymousDbContext();
             IsEnableAnonymous = Convert.ToBoolean(ConfigurationManager.AppSettings["IsAnonymousEnable"]);
        }
        private AnonymousDbContext AnonymousDBContext { get; set; }

        protected byte DomainId { get { return Request.GetDomain(); } }

        /// <summary>
        /// Add an anonymous user to database.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ActionName("DefaultAction")]
        public HttpResponseMessage Create()
        {            
            var fpuserId = 0M;
            try
            {
                if (IsEnableAnonymous)
                    fpuserId = Convert.ToInt64(AnonymousDBContext.Create().SingleOrDefault());
                else
                    fpuserId = Convert.ToInt32(ConfigurationManager.AppSettings["AnonymousCookieDefaultValue"]);
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { FPUserID = fpuserId });
        }

        /// <summary>
        /// Update last visit date time &amp; total no of visit of an anonymous user.
        /// </summary>
        /// <param name="fpuserid"></param>
        /// <returns></returns>
        [HttpPut]
        [ActionName("DefaultAction")]
        public HttpResponseMessage Update(long fpuserid)
        {
            if (!IsEnableAnonymous)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    FPUserID = 0
                });
            }
            AnonymousDetail result = null;
            try
            {
                result = AnonymousDBContext.UpdateLastVisit(fpuserid).SingleOrDefault();                
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
            }

            if (result != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    FPUserID = Convert.ToInt64(result.FPUserID)
                });

            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    FPUserID = 0
                });
            }
        }

        /// <summary>
        /// Update ProfileId &amp; with anonymous id upon any new email or profile signup.
        /// </summary>
        /// <param name="fpuserid"></param>
        /// <param name="fpprofileId"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage UpdateProfileId(long fpuserid, long fpprofileId)
        {
            if (!IsEnableAnonymous)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    FPUserID = 0
                });
            }
            var result =0;
            try
            {
                result = AnonymousDBContext.AssignProfileID(fpuserid, fpprofileId);                
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
            }
            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                FPUserID = result
            });
        }

        /// <summary>
        /// Update anonymous id with profilesid using personguid. Can be used in case of any new signup 
        /// </summary>
        /// <param name="fpuserid"></param>
        /// <param name="personGuid"></param>
        /// <returns></returns>
        [HttpPut]
        public HttpResponseMessage UpdateProfileIdBasedOnGuid(long fpuserid, Guid personGuid)
        {
            if (!IsEnableAnonymous)
            {
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    FPUserID = 0
                });
            }
            if (personGuid == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.UserNotFound));
            }
            var result = 0;
            //var person = ProfileDbContext.People.Where(p => p.PersonGuid.Equals(personGuid)).FirstOrDefault();
            var personDetails = ProfileDbContext.PersonDetails.Where(p => p.FinanceServiceGuid.Equals(personGuid)).FirstOrDefault();
            
            //*IMPORTANT--* As per suggestions from Vishal to Bharat (28/7/2014): in UnknownUser table we will store 'PersonId' & NOT 'PersonDetailID' 
            
         //   AuthInfo authInfo = (ProfileDbContext.AuthInfoes.Where(a => a.Person.PersonGuid == personGuid && a.DomainId == DomainId)).FirstOrDefault();
            if (personDetails == null)
                return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.UserNotFound));
            try
            {
                result = AnonymousDBContext.AssignProfileID(fpuserid, personDetails.PersonId);
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
            }
            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                FPUserID = result
            });
        }
        /// <summary>
        /// Get detail of a person based on personguid
        /// </summary>
        /// <param name="personGuid"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("DefaultAction")]
        public HttpResponseMessage Get(Guid personGuid)
        {
            if (personGuid == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.UserNotFound));
            }

            // var person = ProfileDbContext.People.Where(p => p.PersonGuid.Equals(personGuid)).FirstOrDefault();
            var personDetail  = ProfileDbContext.PersonDetails.Where(p => p.FinanceServiceGuid.Equals(personGuid)).FirstOrDefault();
            if (personDetail == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.UserNotFound)); 
            }
            if (personDetail.DomainId != DomainId)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.UserNotFound));
            }
            //var personDetail = personDetails.Where(p => p.DomainId.Equals(DomainId)).FirstOrDefault();
            
            var anonymousUser = new AnonymousUser()
            {
                DomainId=DomainId,
                // Changes done for Edit_Email.
                Email = /*person.Email*/personDetail.Email,
                FirstName = personDetail.FirstName,
                LastName = personDetail.LastName,
                PersonGuid = personGuid,
                Gender = personDetail.Gender,
                DOB = personDetail.DateOfBirth.GetDecryptNullableDateTime(),
                HomeAirport = personDetail.HomeAirPort
            };
           return  Request.CreateResponse(HttpStatusCode.OK, anonymousUser);            
        }

        /// <summary>
        /// Get person detail with email address.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("DefaultAction")]
        public HttpResponseMessage Get(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.InvalidEmailAddress));
            }
            else if (!ValidationHelper.ValidateEmail(emailAddress))
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, new HttpError(ErrorInfo.InvalidEmailAddress));
            }

            //*IMPORTANT--* As per suggestions from Vishal to Bharat (28/7/2014): in response we will send 'PersonId' & NOT 'PersonDetailID' 
            //*IMPORTANT--* Reason: because we are storing personId and personGUID in cookie (see method: UpdateCookieAndMapUser(int personId_, Guid? personGuid_) in AnonymousTrackingHelper.cs file.

            //AuthInfo authInfo = (ProfileDbContext.AuthInfoes.Where(a => a.Person.Email == emailAddress && a.DomainId == DomainId)).FirstOrDefault();
            //if (authInfo == null)
            //    return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.UserNotFound));            
            //return Request.CreateResponse(HttpStatusCode.OK, new { PersonId = authInfo.PersonId, PersonGuId = authInfo.Person.PersonGuid });

            // Changes done for Edit_Email.
            //var person = ProfileDbContext.People.Where(p => p.Email.Equals(emailAddress)).FirstOrDefault();
            
            //* Commented : As we need FinanceServiceGUID instead of PersonGuid as a response.

           // var person = CommonMethods.GetPersonByEmail(emailAddress, ProfileDbContext);
            var personDetail = ProfileDbContext.PersonDetails.Where(p => p.Email.Equals(emailAddress) && p.DomainId == DomainId).FirstOrDefault();
            if (personDetail == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, new HttpError(ErrorInfo.UserNotFound));
            }

            return Request.CreateResponse(HttpStatusCode.OK, new { PersonId = personDetail.PersonId, PersonGuId = personDetail.FinanceServiceGuid });
        }

        protected override void Dispose(bool disposing)
        {
            if (AnonymousDBContext!=null) {
                AnonymousDBContext.Dispose();
            }
            base.Dispose(disposing);
        }

        
    }
}

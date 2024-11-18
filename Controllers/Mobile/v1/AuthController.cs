﻿using AonFreelancing.Contexts;
using AonFreelancing.Models;
using AonFreelancing.Models.DTOs;
using AonFreelancing.Models.Requests;
using AonFreelancing.Models.Responses;
using AonFreelancing.Services;
using AonFreelancing.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML.Messaging;
using Twilio.Types;

namespace AonFreelancing.Controllers.Mobile.v1
{
    [Route("api/mobile/v1/auth")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly MainAppContext _mainAppContext;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly OTPManager _otpManager;
        private readonly JwtService _jwtService;
        public AuthController(
            UserManager<User> userManager,
            MainAppContext mainAppContext,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            OTPManager otpManager,
            JwtService jwtService
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mainAppContext = mainAppContext;
            _configuration = configuration;
            _otpManager = otpManager;
            _jwtService = jwtService;
        }


        [HttpPost("RegisterByPhone")]

        public async Task <IActionResult> RegisterPhoneNumberAsync([FromBody] RegistByPhoneNumberDTO reg) 
        {
            var user= await _mainAppContext.TemUsers.Where(ph=>ph.phoneNumber==reg.PhoneNumber).FirstOrDefaultAsync();


            if (user != null && !user.PhoneNumberConfirm==false)
            {
                
                return Unauthorized(CreateErrorResponse(StatusCodes.Status401Unauthorized.ToString(), "Verify Your Account First"));
            }

            user = new TemUser() { phoneNumber=reg.PhoneNumber };

           await _mainAppContext.TemUsers.AddAsync(user);

            string otpCode = _otpManager.GenerateOtp();
            await _mainAppContext.SaveChangesAsync();

            OTP otp = new OTP()
            {
                Code = otpCode,
                PhoneNumber = reg.PhoneNumber,
                CreatedDate = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(1),
            };
           

            //send the otp to the specified phone number
            await _otpManager.SendOTPAsync(otp.Code, reg.PhoneNumber);
            await _mainAppContext.OTPs.AddAsync(otp);
            await _mainAppContext.SaveChangesAsync();
          


            return Ok();


        }


        [HttpPost("completeRegister")]

        public async Task<IActionResult> completeRegister([FromBody]CompletRegister re)
        {


            var user = new User()
            {

                Email = re.EmailAddress,
              
                Name = re.Name,

                PhoneNumber=re.PhoneNumber
            };
          await  _userManager.CreateAsync(user,re.Password);

            var ApiResponce = new ApiResponse<object>()
            {
                IsSuccess = true,
                Results = user,
                Errors = []
            };
            return Ok (ApiResponce);




        }
            
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegRequest regRequest)
        {

            User user = new User();
            if (regRequest.UserType == Constants.USER_TYPE_FREELANCER)
            {
                // Register User
                user = new Freelancer
                {
                    Name = regRequest.Name,
                    UserName = regRequest.Username,
                    PhoneNumber = regRequest.PhoneNumber,
                    Skills = regRequest.Skills
                };
            }
            else if (regRequest.UserType == Constants.USER_TYPE_CLIENT)
            {
                user = new Models.Client
                {
                    Name = regRequest.Name,
                    UserName = regRequest.Username,
                    PhoneNumber = regRequest.PhoneNumber,
                    CompanyName = regRequest.CompanyName
                };
            }
            //check if username or phoneNumber is taken
            if (await _userManager.Users.Where(u => u.UserName == regRequest.Username || u.PhoneNumber == regRequest.PhoneNumber).FirstOrDefaultAsync() != null)
                return BadRequest(CreateErrorResponse(StatusCodes.Status400BadRequest.ToString(), "username or phone number is already used by an account"));
            
            //create new User with hashed passworrd in the database
            var userCreationResult = await _userManager.CreateAsync(user, regRequest.Password);
            if (!userCreationResult.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>()
                {
                    Errors = userCreationResult.Errors
                    .Select(e => new Error()
                    {
                        Code = e.Code,
                        Message = e.Description
                    })
                    .ToList()
                });

            // To be fixed
            //assign a role to the newly created User
            //var role = new ApplicationRole { Name = regRequest.UserType };
            //await _roleManager.CreateAsync(role);
            var role = await _roleManager.FindByNameAsync(regRequest.UserType);
            await _userManager.AddToRoleAsync(user, role.Name);

            string otpCode = _otpManager.GenerateOtp();
            //persist the otp to the otps table
            OTP otp = new OTP()
            {
                Code = otpCode,
                PhoneNumber = regRequest.PhoneNumber,
                CreatedDate = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(1),
            };
            await _mainAppContext.OTPs.AddAsync(otp);
            await _mainAppContext.SaveChangesAsync();

            //send the otp to the specified phone number
            await _otpManager.SendOTPAsync(otp.Code, otp.PhoneNumber);

            // Get created User (if it is a freelancer)
            if (regRequest.UserType == Constants.USER_TYPE_FREELANCER)
            {
                var createdUser = await _mainAppContext.Users.OfType<Freelancer>()
                        .Where(u => u.UserName == regRequest.Username)
                        .Select(u => new FreelancerResponseDTO()
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Username = u.UserName,
                            PhoneNumber = u.PhoneNumber,
                            Skills = u.Skills,
                            UserType = Constants.USER_TYPE_FREELANCER,
                            Role = new RoleResponseDTO { Id = role.Id, Name = role.Name }
                        })
                        .FirstOrDefaultAsync();
                return Ok(CreateSuccessResponse(createdUser));
            }
            // Get created User (if it is a client)
            else if (regRequest.UserType == Constants.USER_TYPE_CLIENT)
            {
                var createdUser = await _mainAppContext.Users.OfType<Models.Client>()
                          .Where(c => c.UserName == regRequest.Username)
                          .Select(c => new ClientResponseDTO
                          {
                              Id = c.Id,
                              Name = c.Name,
                              Username = c.UserName,
                              PhoneNumber = c.PhoneNumber,
                              CompanyName = c.CompanyName,
                              UserType = Constants.USER_TYPE_CLIENT,
                              Role = new RoleResponseDTO { Id = role.Id, Name = role.Name }
                          })
                          .FirstOrDefaultAsync();
                return Ok(CreateSuccessResponse(createdUser));
            }
            //this fallback return value will not be returned due to model validation.
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] AuthRequest Req)
        {
            var user = await _userManager.FindByNameAsync(Req.UserName);
            if (user != null && await _userManager.CheckPasswordAsync(user, Req.Password))
            {
                if (!await _userManager.IsPhoneNumberConfirmedAsync(user))
                    return Unauthorized(CreateErrorResponse(StatusCodes.Status401Unauthorized.ToString(), "Verify Your Account First"));

                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
                var token = _jwtService.GenerateJWT(user, role);
                return Ok(CreateSuccessResponse(new LoginResponse()
                {
                    AccessToken = token,
                    UserDetailsDTO = new UserDetailsDTO(user, role)
                }));
            }

            return Unauthorized(CreateErrorResponse(StatusCodes.Status401Unauthorized.ToString(), "UnAuthorized"));
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAsync([FromBody] VerifyReq verifyReq)
        {
            var user = await _mainAppContext.TemUsers.Where(x => x.phoneNumber == verifyReq.Phone).FirstOrDefaultAsync();
            if (user != null && ! user.PhoneNumberConfirm==false)
            {
                OTP? otp = await _mainAppContext.OTPs.Where(o => o.PhoneNumber == verifyReq.Phone).FirstOrDefaultAsync();

                // verify OTP
                if (otp != null && verifyReq.Otp.Equals(otp.Code) && DateTime.Now < otp.ExpiresAt)
                {
                    user.PhoneNumberConfirm = true;
                     _mainAppContext.Update(user);

                    // disable sent OTP
                    otp.IsUsed = true;
                     _mainAppContext.Update(otp);
                    await _mainAppContext.SaveChangesAsync();

                    return Ok(CreateSuccessResponse("Activated"));
                }
            }
            return Unauthorized(CreateErrorResponse(StatusCodes.Status401Unauthorized.ToString(), "UnAuthorized"));
        }



        [HttpGet("PhoneNumber")]

        public async Task<IActionResult> GetProfile(string PhoneNumber)
        {
            var User = _userManager.Users.Where(p => p.PhoneNumber == PhoneNumber).FirstOrDefault();

            if (User != null)
            {
                var user = await _userManager.Users.Include(p => p.projects).Select(async U => new UserProfileDTO



                {

                    PhoneNumber = User.PhoneNumber,
                    Email = User.Email,
                    id = User.Id,
                    name = User.Name,
                    About = U.projects.Select(p => new ProjectProfileDTO
                    {


                        Id = p.Id,
                        Description = p.Description,
                        startDate = DateTime.Now,
                        endDate = DateTime.Now,

                    }).ToList()


                }).ToListAsync();

                var ApiRespons = new ApiResponse<object>()
                {
                    IsSuccess = true,
                    Results = user,
                    Errors = []
                };
                return Ok(ApiRespons);  
            }
            return NotFound();



        }

        //[HttpPost("forgotpassword")]
        //public async Task<IActionResult> ForgotPasswordMethod([FromBody] ForgotPasswordReq forgotPasswordRequest)
        //{
        //    var user = await _userManager.Users.Where(u => u.PhoneNumber == forgotPasswordRequest.PhoneNumber).FirstOrDefaultAsync();
        //    if (user != null)
        //    {
        //        string token = await _userManager.GeneratePasswordResetTokenAsync(user);

        //        //send whatsapp  message containing the reset password url
        //        await _otpManager.sendForgotPasswordMessageAsync(token, user.PhoneNumber);
        //    }
        //    // to maintain confidentiality, we always return an OK response even if the user was not found. 
        //    return Ok(CreateSuccessResponse("Check your WhatsApp inbox for password reset token."));
        //}

        //[HttpPost("resetpassword")]
        //public async Task<IActionResult> ResetPasswordMethod([FromBody] ResetPasswordReq resetPasswordReq)
        //{
        //    User? user = await _userManager.Users.Where(u => u.PhoneNumber == resetPasswordReq.PhoneNumber).FirstOrDefaultAsync();
        //    if (user != null)
        //        await _userManager.ResetPasswordAsync(user, resetPasswordReq.Token, resetPasswordReq.Password);

        //    // to maintain confidentiality, we always return an OK response even if the user was not found. 
        //    return Ok(CreateSuccessResponse("Your password have been reset"));
        //}
    }
}

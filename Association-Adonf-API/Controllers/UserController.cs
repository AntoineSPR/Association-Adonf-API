using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using AssociationAdonfAPI.Models;
using AssociationAdonfAPI.Services;

namespace AssociationAdonfAPI.Controllers
{
    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly AuthService authService;
        private readonly UserService userService;
        private readonly SendMailService mailService;

        public UserController(
            AuthService authService,
            UserService userService,
            SendMailService mailService
        )
        {
            this.authService = authService;
            this.userService = userService;
            this.mailService = mailService;
        }

        [AllowAnonymous]
        [EnableCors]
        [Route("register")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] UserCreateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    throw new Exception("Validation issue");
                }

                var result = await authService.Register(model);

                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [EnableCors]
        [Route("update")]
        [HttpPatch]
        public async Task<IActionResult> Update([FromBody] UserCreateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await authService.Update(model, HttpContext.User);

                return Ok(result);
            }
            catch
            {
                throw;
            }

        }


        [EnableCors]
        [Route("change-password")]
        [HttpPut]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO passwordData)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await authService.ChangePassword(passwordData, HttpContext.User);

                return Ok(new { message = "Mot de passe modifié avec succès" });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message });
            }
        }


        [AllowAnonymous]
        [Route("login")]
        [HttpPost]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] UserLoginDTO model)
        {
            try
            {
                if (!ModelState.IsValid) { throw new Exception("Login failed"); }
                ;

                var result = await authService.Login(model);

                return Ok(result);

            }
            catch
            {
                return new StatusCodeResult(StatusCodes.Status400BadRequest);
            }
        }

        [Route("email/{email}")]
        [HttpGet]
        public async Task<ActionResult<UserResponseDTO?>> GetUserByEmail([FromRoute] string email)
        {
            UserResponseDTO? user = await userService.GetUserByEmail(email);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [AllowAnonymous]
        [EnableCors]
        [Route("forgot-password/{email}")]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromRoute] string email)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await mailService.SendPasswordResetEmail(email);

                return Ok(new { message = "Si votre email existe, un lien de réinitialisation a été envoyé." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        [AllowAnonymous]
        [EnableCors]
        [Route("reset-password")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await authService.ResetPassword(model);

                return Ok(new { message = "Mot de passe réinitialisé avec succès." });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message });
            }
        }

        //[AllowAnonymous]
        //[EnableCors]
        //[Route("send-mail")]
        //[HttpPost]
        //public async Task<IActionResult> SendMail([FromBody] Mail mail)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        await mailService.SendEmail(mail);

        //        return Ok(new { message = "Email envoyé." });
        //    }
        //    catch (Exception e)
        //    {
        //        return BadRequest(new { message = e.Message });
        //    }
        //}
    }
}

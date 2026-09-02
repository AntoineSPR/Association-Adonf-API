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
        private readonly ILogger<UserController> logger;

        public UserController(
            AuthService authService,
            UserService userService,
            SendMailService mailService,
            ILogger<UserController> logger
        )
        {
            this.authService = authService;
            this.userService = userService;
            this.mailService = mailService;
            this.logger = logger;
        }

        [Authorize(Roles = "Admin")]
        [EnableCors]
        [Route("register")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] UserCreateDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    throw new Exception("Données de validation invalides");
                }

                var result = await authService.Register(model);

                return Ok(result);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message });
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
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Veuillez renseigner un email valide et un mot de passe." });
            }

            try
            {
                var result = await authService.Login(model);

                return Ok(result);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(new { message = e.Message });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur est survenue. Veuillez réessayer." });
            }
        }

        [AllowAnonymous]
        [EnableCors]
        [Route("refresh")]
        [HttpPost]
        public async Task<ActionResult<LoginResponseDTO>> Refresh([FromBody] RefreshRequestDTO model)
        {
            try
            {
                var result = await authService.RefreshAsync(model.RefreshToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException e)
            {
                return Unauthorized(new { message = e.Message });
            }
            catch
            {
                return Unauthorized(new { message = "Session expirée, veuillez vous reconnecter" });
            }
        }

        [AllowAnonymous]
        [EnableCors]
        [Route("logout")]
        [HttpPost]
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDTO model)
        {
            await authService.RevokeRefreshTokenAsync(model.RefreshToken);
            return Ok();
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
                logger.LogError(e, "Échec de l'envoi de l'email de réinitialisation de mot de passe pour {Email}", email);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Une erreur est survenue lors de l'envoi de l'email. Veuillez réessayer plus tard." });
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

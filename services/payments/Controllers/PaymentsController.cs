using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Api.Security;
using Payments.Api.Services;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentsService paymentsService;

    public PaymentsController(IPaymentsService paymentsService)
    {
        this.paymentsService = paymentsService;
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicies.PaymentsRead)]
    public async Task<IActionResult> GetPayments() => Ok(await paymentsService.GetPaymentsAsync());

    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        var payment = await paymentsService.GetPaymentByIdAsync(paymentId);
        if (payment is null || (!CanReadAllPayments() && !IsCurrentUser(payment.UserId))) return NotFound();
        return Ok(payment);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetPaymentsByOrderId(Guid orderId)
    {
        var payments = await paymentsService.GetPaymentsByOrderIdAsync(orderId);
        if (!CanReadAllPayments() && payments.Any(payment => !IsCurrentUser(payment.UserId))) return NotFound();
        return Ok(payments);
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicies.PaymentsCreate)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        if (!CanManagePayments())
        {
            request.UserId = userId;
            request.Status = "Pending";
        }

        var payment = await paymentsService.CreatePaymentAsync(request);
        return CreatedAtAction(nameof(GetPaymentById), new { paymentId = payment.Id }, payment);
    }

    [HttpPatch("{paymentId:guid}/status")]
    [Authorize(Policy = PermissionPolicies.PaymentsManage)]
    public async Task<IActionResult> UpdatePaymentStatus(Guid paymentId, [FromBody] UpdatePaymentStatusRequest request)
    {
        return await paymentsService.UpdatePaymentStatusAsync(paymentId, request) ? NoContent() : NotFound();
    }

    [HttpPost("{paymentId:guid}/refund")]
    [Authorize(Policy = PermissionPolicies.PaymentsRefund)]
    public async Task<IActionResult> RefundPayment(Guid paymentId, [FromBody] RefundPaymentRequest request)
    {
        var result = await paymentsService.RefundPaymentAsync(paymentId, request);
        if (result.NotFound)
        {
            return NotFound();
        }

        return result.IsSuccess ? Accepted(result.Payment) : BadRequest(result.Error);
    }

    [HttpPost("orders/{orderId:guid}/refund")]
    [Authorize(Policy = PermissionPolicies.OrderCancellation)]
    public async Task<IActionResult> RefundOrCancelOrderPayments(Guid orderId)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var updated = await paymentsService.RefundOrCancelOrderPaymentsAsync(orderId, userId);
        return updated ? NoContent() : NotFound();
    }

    private bool CanReadAllPayments() => User.HasClaim("permission", "payments.read");

    private bool CanManagePayments() => User.HasClaim("permission", "payments.manage");

    private bool IsCurrentUser(Guid userId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) &&
        currentUserId == userId;
}

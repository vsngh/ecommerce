using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Api.Services;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentsService paymentsService;

    public PaymentsController(IPaymentsService paymentsService)
    {
        this.paymentsService = paymentsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments() => Ok(await paymentsService.GetPaymentsAsync());

    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        var payment = await paymentsService.GetPaymentByIdAsync(paymentId);
        return payment is null ? NotFound() : Ok(payment);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetPaymentsByOrderId(Guid orderId)
    {
        return Ok(await paymentsService.GetPaymentsByOrderIdAsync(orderId));
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var payment = await paymentsService.CreatePaymentAsync(request);
        return CreatedAtAction(nameof(GetPaymentById), new { paymentId = payment.Id }, payment);
    }

    [HttpPatch("{paymentId:guid}/status")]
    public async Task<IActionResult> UpdatePaymentStatus(Guid paymentId, [FromBody] UpdatePaymentStatusRequest request)
    {
        return await paymentsService.UpdatePaymentStatusAsync(paymentId, request) ? NoContent() : NotFound();
    }

    [HttpPost("{paymentId:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid paymentId, [FromBody] RefundPaymentRequest request)
    {
        var result = await paymentsService.RefundPaymentAsync(paymentId, request);
        if (result.NotFound)
        {
            return NotFound();
        }

        return result.IsSuccess ? Accepted(result.Payment) : BadRequest(result.Error);
    }
}

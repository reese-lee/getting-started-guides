package com.example.demo;

import io.opentelemetry.api.GlobalOpenTelemetry;
import io.opentelemetry.api.common.Attributes;
import io.opentelemetry.api.common.AttributeKey;

// Imports for tracing
import io.opentelemetry.api.trace.Tracer;
import io.opentelemetry.api.trace.StatusCode;
import io.opentelemetry.api.trace.Span;

// Imports for metrics
import io.opentelemetry.api.metrics.Meter;
import io.opentelemetry.api.metrics.LongCounter;

// Imports for logging
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.HttpRequestMethodNotSupportedException;
import org.springframework.web.bind.MissingServletRequestParameterException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Random;
import java.util.Map;

@RestController
public class Controller {

  // Tracing constants
  private static final Tracer TRACER = GlobalOpenTelemetry.getTracer(Controller.class.getName());
 private static final AttributeKey<Long> ATTR_N = AttributeKey.longKey("fibonacci.n");
 private static final AttributeKey<Long> ATTR_RESULT = AttributeKey.longKey("fibonacci.result");

 // Meter constants
 private static final Meter METER =
 GlobalOpenTelemetry.getMeterProvider().get(Application.class.getName());
 private final LongCounter MY_COUNTER =
 METER.counterBuilder("my-custom-counter").setDescription("A counter to count things").build();

 // Logging constant (note that it is not an OTel component)
 private static final Logger LOGGER = LogManager.getLogger(Controller.class);
 
  @GetMapping(value = "/fibonacci")
  public Map<String, Object> ping(@RequestParam(required = true, name = "n") long n) {
    return Map.of("n", n, "result", fibonacci(n));
  }

  /**
   * Compute the fibonacci number for {@code n}.
   *
   * @param n must be >=1 and <= 90.
   */

  private long fibonacci(long n) {
    var span = TRACER.spanBuilder("fibonacci").startSpan();
    span.setAttribute(ATTR_N, n);
    
    // A custom counter
    MY_COUNTER.add(123, Attributes.of(AttributeKey.stringKey("MyKey"), "SomeValue"));

    // A sample log
    LOGGER.info("A sample log message!");
    try (var scope = span.makeCurrent()) {      
      if (n < 1 || n > 90) {
        throw new IllegalArgumentException("n must be 1 <= n <= 90.");
      }

      // Base cases
      if (n == 1) {
        span.setAttribute(ATTR_RESULT, 1);
        return 1;
      }
      if (n == 2) {
        span.setAttribute(ATTR_RESULT, 1);
        return 1;
      }

      long lastLast = 1;
      long last = 2;
      for (long i = 4; i <= n; i++) {
        long cur = last + lastLast;
        lastLast = last;
        last = cur;
      }
      span.setAttribute(ATTR_RESULT, last);
      return last;
    } catch (IllegalArgumentException e) {
      // Record the exception and set the span status
      span.recordException(e)
        .setStatus(StatusCode.ERROR, e.getMessage());
      throw e;
    } finally {
        span.end();
    }
  }

  @ControllerAdvice
  private static class ErrorHandler {

    @ExceptionHandler({
            IllegalArgumentException.class,
            MissingServletRequestParameterException.class,
            HttpRequestMethodNotSupportedException.class
    })
    public ResponseEntity<Object> handleException(Exception e) {
      Span.current().setStatus(StatusCode.ERROR, e.getMessage());
      return new ResponseEntity<>(Map.of("message", e.getMessage()), HttpStatus.BAD_REQUEST);
    }

  }
}

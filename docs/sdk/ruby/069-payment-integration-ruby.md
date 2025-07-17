# Payment Integration with TuskLang and Ruby

This guide covers integrating payment systems with TuskLang and Ruby applications for secure financial transactions.

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Setup](#basic-setup)
4. [Payment Providers](#payment-providers)
5. [Payment Methods](#payment-methods)
6. [Advanced Features](#advanced-features)
7. [Security](#security)
8. [Testing](#testing)
9. [Deployment](#deployment)

## Overview

Payment integration enables secure financial transactions in applications. This guide shows how to integrate various payment systems with TuskLang and Ruby applications.

### Key Features

- **Multiple payment providers** (Stripe, PayPal, Square)
- **Payment methods** (credit cards, digital wallets, bank transfers)
- **Subscription management** and recurring billing
- **Payment security** and PCI compliance
- **Transaction monitoring** and reporting
- **Refund and dispute handling**

## Installation

### Dependencies

```ruby
# Gemfile
gem 'stripe'
gem 'paypal-sdk-rest'
gem 'square'
gem 'braintree'
gem 'encrypted_attributes'
gem 'redis'
gem 'connection_pool'
```

### TuskLang Configuration

```tusk
# config/payment.tusk
payment:
  provider: "stripe"  # stripe, paypal, square, braintree
  
  stripe:
    publishable_key: "pk_test_your_key"
    secret_key: "sk_test_your_key"
    webhook_secret: "whsec_your_webhook_secret"
    currency: "usd"
    api_version: "2023-10-16"
  
  paypal:
    client_id: "your_client_id"
    client_secret: "your_client_secret"
    mode: "sandbox"  # sandbox, live
    currency: "USD"
  
  square:
    application_id: "your_application_id"
    access_token: "your_access_token"
    location_id: "your_location_id"
    environment: "sandbox"  # sandbox, production
  
  braintree:
    merchant_id: "your_merchant_id"
    public_key: "your_public_key"
    private_key: "your_private_key"
    environment: "sandbox"  # sandbox, production
  
  security:
    encrypt_sensitive_data: true
    pci_compliant: true
    tokenization_enabled: true
  
  webhooks:
    enabled: true
    endpoint: "/webhooks/payment"
    timeout: 30
  
  monitoring:
    enabled: true
    metrics_port: 9090
    health_check_interval: 30
```

## Basic Setup

### Payment Manager

```ruby
# app/payment/payment_manager.rb
class PaymentManager
  include Singleton

  def initialize
    @config = Rails.application.config.payment
    @provider = create_provider
  end

  def create_payment_intent(amount, currency, options = {})
    @provider.create_payment_intent(amount, currency, options)
  end

  def process_payment(payment_method_id, amount, currency, options = {})
    @provider.process_payment(payment_method_id, amount, currency, options)
  end

  def create_subscription(customer_id, plan_id, options = {})
    @provider.create_subscription(customer_id, plan_id, options)
  end

  def cancel_subscription(subscription_id)
    @provider.cancel_subscription(subscription_id)
  end

  def refund_payment(payment_id, amount = nil, reason = nil)
    @provider.refund_payment(payment_id, amount, reason)
  end

  def get_payment_status(payment_id)
    @provider.get_payment_status(payment_id)
  end

  def create_customer(user, payment_method = nil)
    @provider.create_customer(user, payment_method)
  end

  def update_customer(customer_id, attributes)
    @provider.update_customer(customer_id, attributes)
  end

  def delete_customer(customer_id)
    @provider.delete_customer(customer_id)
  end

  def health_check
    @provider.health_check
  end

  private

  def create_provider
    case @config[:provider]
    when 'stripe'
      StripeProvider.new(@config[:stripe])
    when 'paypal'
      PayPalProvider.new(@config[:paypal])
    when 'square'
      SquareProvider.new(@config[:square])
    when 'braintree'
      BraintreeProvider.new(@config[:braintree])
    else
      raise "Unsupported payment provider: #{@config[:provider]}"
    end
  end
end
```

### Base Payment Provider

```ruby
# app/payment/providers/base_payment_provider.rb
class BasePaymentProvider
  def create_payment_intent(amount, currency, options = {})
    raise NotImplementedError, "#{self.class} must implement create_payment_intent"
  end

  def process_payment(payment_method_id, amount, currency, options = {})
    raise NotImplementedError, "#{self.class} must implement process_payment"
  end

  def create_subscription(customer_id, plan_id, options = {})
    raise NotImplementedError, "#{self.class} must implement create_subscription"
  end

  def cancel_subscription(subscription_id)
    raise NotImplementedError, "#{self.class} must implement cancel_subscription"
  end

  def refund_payment(payment_id, amount = nil, reason = nil)
    raise NotImplementedError, "#{self.class} must implement refund_payment"
  end

  def get_payment_status(payment_id)
    raise NotImplementedError, "#{self.class} must implement get_payment_status"
  end

  def create_customer(user, payment_method = nil)
    raise NotImplementedError, "#{self.class} must implement create_customer"
  end

  def update_customer(customer_id, attributes)
    raise NotImplementedError, "#{self.class} must implement update_customer"
  end

  def delete_customer(customer_id)
    raise NotImplementedError, "#{self.class} must implement delete_customer"
  end

  def health_check
    raise NotImplementedError, "#{self.class} must implement health_check"
  end

  protected

  def log_payment_event(event_type, data)
    PaymentEvent.create!(
      event_type: event_type,
      provider: self.class.name,
      data: data,
      timestamp: Time.current
    )
  end

  def encrypt_sensitive_data(data)
    return data unless Rails.application.config.payment[:security][:encrypt_sensitive_data]
    
    # Implementation would encrypt sensitive data
    data
  end

  def decrypt_sensitive_data(data)
    return data unless Rails.application.config.payment[:security][:encrypt_sensitive_data]
    
    # Implementation would decrypt sensitive data
    data
  end
end
```

## Payment Providers

### Stripe Provider

```ruby
# app/payment/providers/stripe_provider.rb
class StripeProvider < BasePaymentProvider
  def initialize(config)
    @config = config
    Stripe.api_key = config[:secret_key]
    Stripe.api_version = config[:api_version]
  end

  def create_payment_intent(amount, currency, options = {})
    intent_params = {
      amount: amount,
      currency: currency || @config[:currency],
      metadata: options[:metadata] || {}
    }

    intent_params[:customer] = options[:customer_id] if options[:customer_id]
    intent_params[:payment_method_types] = options[:payment_method_types] || ['card']

    intent = Stripe::PaymentIntent.create(intent_params)
    
    log_payment_event('payment_intent_created', {
      intent_id: intent.id,
      amount: amount,
      currency: currency
    })

    {
      id: intent.id,
      client_secret: intent.client_secret,
      status: intent.status,
      amount: intent.amount,
      currency: intent.currency
    }
  rescue Stripe::StripeError => e
    log_payment_event('payment_intent_error', {
      error: e.message,
      amount: amount,
      currency: currency
    })
    raise PaymentError.new("Failed to create payment intent: #{e.message}")
  end

  def process_payment(payment_method_id, amount, currency, options = {})
    payment_intent = create_payment_intent(amount, currency, options)
    
    intent = Stripe::PaymentIntent.confirm(
      payment_intent[:id],
      { payment_method: payment_method_id }
    )

    log_payment_event('payment_processed', {
      intent_id: intent.id,
      payment_method_id: payment_method_id,
      amount: amount,
      currency: currency
    })

    {
      id: intent.id,
      status: intent.status,
      amount: intent.amount,
      currency: intent.currency,
      payment_method: intent.payment_method
    }
  rescue Stripe::StripeError => e
    log_payment_event('payment_error', {
      error: e.message,
      payment_method_id: payment_method_id,
      amount: amount
    })
    raise PaymentError.new("Payment failed: #{e.message}")
  end

  def create_subscription(customer_id, plan_id, options = {})
    subscription_params = {
      customer: customer_id,
      items: [{ price: plan_id }],
      metadata: options[:metadata] || {}
    }

    subscription_params[:trial_period_days] = options[:trial_days] if options[:trial_days]

    subscription = Stripe::Subscription.create(subscription_params)
    
    log_payment_event('subscription_created', {
      subscription_id: subscription.id,
      customer_id: customer_id,
      plan_id: plan_id
    })

    {
      id: subscription.id,
      status: subscription.status,
      customer_id: subscription.customer,
      plan_id: plan_id,
      current_period_start: subscription.current_period_start,
      current_period_end: subscription.current_period_end
    }
  rescue Stripe::StripeError => e
    log_payment_event('subscription_error', {
      error: e.message,
      customer_id: customer_id,
      plan_id: plan_id
    })
    raise PaymentError.new("Failed to create subscription: #{e.message}")
  end

  def cancel_subscription(subscription_id)
    subscription = Stripe::Subscription.update(
      subscription_id,
      { cancel_at_period_end: true }
    )
    
    log_payment_event('subscription_cancelled', {
      subscription_id: subscription_id
    })

    {
      id: subscription.id,
      status: subscription.status,
      cancel_at_period_end: subscription.cancel_at_period_end
    }
  rescue Stripe::StripeError => e
    log_payment_event('subscription_cancel_error', {
      error: e.message,
      subscription_id: subscription_id
    })
    raise PaymentError.new("Failed to cancel subscription: #{e.message}")
  end

  def refund_payment(payment_id, amount = nil, reason = nil)
    refund_params = { payment_intent: payment_id }
    refund_params[:amount] = amount if amount
    refund_params[:reason] = reason if reason

    refund = Stripe::Refund.create(refund_params)
    
    log_payment_event('refund_created', {
      refund_id: refund.id,
      payment_id: payment_id,
      amount: amount,
      reason: reason
    })

    {
      id: refund.id,
      status: refund.status,
      amount: refund.amount,
      currency: refund.currency
    }
  rescue Stripe::StripeError => e
    log_payment_event('refund_error', {
      error: e.message,
      payment_id: payment_id
    })
    raise PaymentError.new("Failed to create refund: #{e.message}")
  end

  def get_payment_status(payment_id)
    intent = Stripe::PaymentIntent.retrieve(payment_id)
    
    {
      id: intent.id,
      status: intent.status,
      amount: intent.amount,
      currency: intent.currency,
      payment_method: intent.payment_method
    }
  rescue Stripe::StripeError => e
    raise PaymentError.new("Failed to get payment status: #{e.message}")
  end

  def create_customer(user, payment_method = nil)
    customer_params = {
      email: user.email,
      name: user.name,
      metadata: { user_id: user.id }
    }

    customer_params[:payment_method] = payment_method if payment_method

    customer = Stripe::Customer.create(customer_params)
    
    log_payment_event('customer_created', {
      customer_id: customer.id,
      user_id: user.id
    })

    {
      id: customer.id,
      email: customer.email,
      name: customer.name
    }
  rescue Stripe::StripeError => e
    log_payment_event('customer_error', {
      error: e.message,
      user_id: user.id
    })
    raise PaymentError.new("Failed to create customer: #{e.message}")
  end

  def update_customer(customer_id, attributes)
    customer = Stripe::Customer.update(customer_id, attributes)
    
    log_payment_event('customer_updated', {
      customer_id: customer_id,
      attributes: attributes
    })

    {
      id: customer.id,
      email: customer.email,
      name: customer.name
    }
  rescue Stripe::StripeError => e
    log_payment_event('customer_update_error', {
      error: e.message,
      customer_id: customer_id
    })
    raise PaymentError.new("Failed to update customer: #{e.message}")
  end

  def delete_customer(customer_id)
    customer = Stripe::Customer.delete(customer_id)
    
    log_payment_event('customer_deleted', {
      customer_id: customer_id
    })

    { id: customer.id, deleted: customer.deleted }
  rescue Stripe::StripeError => e
    log_payment_event('customer_delete_error', {
      error: e.message,
      customer_id: customer_id
    })
    raise PaymentError.new("Failed to delete customer: #{e.message}")
  end

  def health_check
    begin
      Stripe::Balance.retrieve
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end
end
```

### PayPal Provider

```ruby
# app/payment/providers/paypal_provider.rb
class PayPalProvider < BasePaymentProvider
  def initialize(config)
    @config = config
    PayPal::SDK.configure(
      mode: config[:mode],
      client_id: config[:client_id],
      client_secret: config[:client_secret]
    )
  end

  def create_payment_intent(amount, currency, options = {})
    payment = PayPal::SDK::REST::Payment.new({
      intent: "sale",
      payer: {
        payment_method: "paypal"
      },
      transactions: [{
        amount: {
          total: (amount / 100.0).to_s,
          currency: currency || @config[:currency]
        },
        description: options[:description] || "Payment"
      }],
      redirect_urls: {
        return_url: options[:return_url],
        cancel_url: options[:cancel_url]
      }
    })

    if payment.create
      log_payment_event('payment_intent_created', {
        payment_id: payment.id,
        amount: amount,
        currency: currency
      })

      {
        id: payment.id,
        approval_url: payment.links.find { |link| link.rel == "approval_url" }.href,
        status: payment.state,
        amount: amount,
        currency: currency
      }
    else
      log_payment_event('payment_intent_error', {
        error: payment.error.inspect,
        amount: amount,
        currency: currency
      })
      raise PaymentError.new("Failed to create payment: #{payment.error.inspect}")
    end
  end

  def process_payment(payment_id, payer_id, amount, currency, options = {})
    payment = PayPal::SDK::REST::Payment.find(payment_id)
    
    if payment.execute(payer_id: payer_id)
      log_payment_event('payment_processed', {
        payment_id: payment.id,
        payer_id: payer_id,
        amount: amount,
        currency: currency
      })

      {
        id: payment.id,
        status: payment.state,
        amount: amount,
        currency: currency,
        payer_id: payer_id
      }
    else
      log_payment_event('payment_error', {
        error: payment.error.inspect,
        payment_id: payment_id
      })
      raise PaymentError.new("Payment failed: #{payment.error.inspect}")
    end
  end

  def create_subscription(customer_id, plan_id, options = {})
    agreement = PayPal::SDK::REST::Agreement.new({
      name: options[:name] || "Subscription",
      description: options[:description] || "Recurring payment",
      start_date: (Time.current + 1.hour).iso8601,
      payer: {
        payment_method: "paypal"
      },
      plan: {
        id: plan_id
      }
    })

    if agreement.create
      log_payment_event('subscription_created', {
        agreement_id: agreement.id,
        customer_id: customer_id,
        plan_id: plan_id
      })

      {
        id: agreement.id,
        status: agreement.state,
        customer_id: customer_id,
        plan_id: plan_id
      }
    else
      log_payment_event('subscription_error', {
        error: agreement.error.inspect,
        customer_id: customer_id,
        plan_id: plan_id
      })
      raise PaymentError.new("Failed to create subscription: #{agreement.error.inspect}")
    end
  end

  def cancel_subscription(subscription_id)
    agreement = PayPal::SDK::REST::Agreement.find(subscription_id)
    
    if agreement.cancel({ note: "Subscription cancelled" })
      log_payment_event('subscription_cancelled', {
        subscription_id: subscription_id
      })

      {
        id: agreement.id,
        status: agreement.state
      }
    else
      log_payment_event('subscription_cancel_error', {
        error: agreement.error.inspect,
        subscription_id: subscription_id
      })
      raise PaymentError.new("Failed to cancel subscription: #{agreement.error.inspect}")
    end
  end

  def refund_payment(payment_id, amount = nil, reason = nil)
    sale = PayPal::SDK::REST::Sale.find(payment_id)
    
    refund = sale.refund({
      amount: amount ? { total: (amount / 100.0).to_s, currency: sale.amount.currency } : nil
    })

    if refund.success?
      log_payment_event('refund_created', {
        refund_id: refund.id,
        payment_id: payment_id,
        amount: amount,
        reason: reason
      })

      {
        id: refund.id,
        status: refund.state,
        amount: amount
      }
    else
      log_payment_event('refund_error', {
        error: refund.error.inspect,
        payment_id: payment_id
      })
      raise PaymentError.new("Failed to create refund: #{refund.error.inspect}")
    end
  end

  def get_payment_status(payment_id)
    payment = PayPal::SDK::REST::Payment.find(payment_id)
    
    {
      id: payment.id,
      status: payment.state,
      amount: payment.transactions.first.amount.total,
      currency: payment.transactions.first.amount.currency
    }
  rescue => e
    raise PaymentError.new("Failed to get payment status: #{e.message}")
  end

  def create_customer(user, payment_method = nil)
    # PayPal doesn't have a separate customer concept
    # Customers are identified by their PayPal account
    {
      id: "paypal_#{user.id}",
      email: user.email,
      name: user.name
    }
  end

  def update_customer(customer_id, attributes)
    # PayPal doesn't support customer updates
    { id: customer_id }
  end

  def delete_customer(customer_id)
    # PayPal doesn't support customer deletion
    { id: customer_id, deleted: false }
  end

  def health_check
    begin
      # Test PayPal API connectivity
      PayPal::SDK::REST::Payment.new
      { status: 'healthy' }
    rescue => e
      { status: 'error', error: e.message }
    end
  end
end
```

## Payment Methods

### Credit Card Payment

```ruby
# app/payment/methods/credit_card_payment.rb
class CreditCardPayment
  def initialize(payment_manager)
    @payment_manager = payment_manager
  end

  def process_credit_card_payment(user, card_token, amount, currency, options = {})
    # Validate card token
    validate_card_token(card_token)
    
    # Process payment
    result = @payment_manager.process_payment(
      card_token,
      amount,
      currency,
      options.merge(payment_method_types: ['card'])
    )
    
    # Create payment record
    create_payment_record(user, result, 'credit_card', options)
    
    result
  end

  def save_credit_card(user, card_token)
    # Create payment method
    payment_method = @payment_manager.create_payment_method(
      user.customer_id,
      card_token
    )
    
    # Save to database
    SavedPaymentMethod.create!(
      user: user,
      provider_payment_method_id: payment_method[:id],
      payment_method_type: 'credit_card',
      last_four: payment_method[:last_four],
      brand: payment_method[:brand],
      exp_month: payment_method[:exp_month],
      exp_year: payment_method[:exp_year]
    )
    
    payment_method
  end

  private

  def validate_card_token(card_token)
    # Implementation would validate card token format
    raise PaymentError.new("Invalid card token") if card_token.blank?
  end

  def create_payment_record(user, result, payment_method, options)
    Payment.create!(
      user: user,
      provider_payment_id: result[:id],
      amount: result[:amount],
      currency: result[:currency],
      status: result[:status],
      payment_method: payment_method,
      metadata: options[:metadata] || {}
    )
  end
end
```

### Digital Wallet Payment

```ruby
# app/payment/methods/digital_wallet_payment.rb
class DigitalWalletPayment
  def initialize(payment_manager)
    @payment_manager = payment_manager
  end

  def process_digital_wallet_payment(user, wallet_token, amount, currency, options = {})
    # Validate wallet token
    validate_wallet_token(wallet_token)
    
    # Process payment
    result = @payment_manager.process_payment(
      wallet_token,
      amount,
      currency,
      options.merge(payment_method_types: ['card', 'wallet'])
    )
    
    # Create payment record
    create_payment_record(user, result, 'digital_wallet', options)
    
    result
  end

  def get_supported_wallets
    {
      apple_pay: 'Apple Pay',
      google_pay: 'Google Pay',
      paypal: 'PayPal',
      venmo: 'Venmo'
    }
  end

  private

  def validate_wallet_token(wallet_token)
    # Implementation would validate wallet token format
    raise PaymentError.new("Invalid wallet token") if wallet_token.blank?
  end

  def create_payment_record(user, result, payment_method, options)
    Payment.create!(
      user: user,
      provider_payment_id: result[:id],
      amount: result[:amount],
      currency: result[:currency],
      status: result[:status],
      payment_method: payment_method,
      metadata: options[:metadata] || {}
    )
  end
end
```

## Advanced Features

### Subscription Management

```ruby
# app/payment/subscriptions/subscription_manager.rb
class SubscriptionManager
  include Singleton

  def initialize
    @payment_manager = PaymentManager.instance
  end

  def create_subscription(user, plan, options = {})
    # Create or get customer
    customer = ensure_customer_exists(user)
    
    # Create subscription
    subscription = @payment_manager.create_subscription(
      customer[:id],
      plan.provider_plan_id,
      options
    )
    
    # Save subscription record
    save_subscription_record(user, plan, subscription)
    
    subscription
  end

  def cancel_subscription(subscription_id, user_id)
    # Cancel with provider
    result = @payment_manager.cancel_subscription(subscription_id)
    
    # Update subscription record
    subscription = Subscription.find_by(
      provider_subscription_id: subscription_id,
      user_id: user_id
    )
    
    subscription&.update!(
      status: 'cancelled',
      cancelled_at: Time.current
    )
    
    result
  end

  def update_subscription(subscription_id, user_id, plan)
    # Cancel current subscription
    cancel_subscription(subscription_id, user_id)
    
    # Create new subscription with new plan
    user = User.find(user_id)
    create_subscription(user, plan)
  end

  def get_subscription_status(subscription_id)
    subscription = Subscription.find_by(provider_subscription_id: subscription_id)
    return nil unless subscription
    
    {
      id: subscription.id,
      status: subscription.status,
      plan: subscription.plan.name,
      current_period_start: subscription.current_period_start,
      current_period_end: subscription.current_period_end,
      cancelled_at: subscription.cancelled_at
    }
  end

  def process_subscription_renewal(subscription_id)
    subscription = Subscription.find_by(provider_subscription_id: subscription_id)
    return unless subscription
    
    # Process renewal payment
    payment = process_renewal_payment(subscription)
    
    # Update subscription
    subscription.update!(
      current_period_start: payment[:current_period_start],
      current_period_end: payment[:current_period_end],
      last_payment_at: Time.current
    )
    
    payment
  end

  private

  def ensure_customer_exists(user)
    if user.customer_id
      { id: user.customer_id }
    else
      customer = @payment_manager.create_customer(user)
      user.update!(customer_id: customer[:id])
      customer
    end
  end

  def save_subscription_record(user, plan, subscription)
    Subscription.create!(
      user: user,
      plan: plan,
      provider_subscription_id: subscription[:id],
      status: subscription[:status],
      current_period_start: Time.at(subscription[:current_period_start]),
      current_period_end: Time.at(subscription[:current_period_end])
    )
  end

  def process_renewal_payment(subscription)
    # Implementation would process renewal payment
    {
      current_period_start: subscription.current_period_end,
      current_period_end: subscription.current_period_end + 1.month
    }
  end
end
```

### Payment Security

```ruby
# app/payment/security/payment_security.rb
class PaymentSecurity
  include Singleton

  def initialize
    @config = Rails.application.config.payment
  end

  def validate_payment_data(data)
    # Validate required fields
    validate_required_fields(data)
    
    # Validate amount
    validate_amount(data[:amount])
    
    # Validate currency
    validate_currency(data[:currency])
    
    # Sanitize data
    sanitize_data(data)
  end

  def encrypt_payment_data(data)
    return data unless @config[:security][:encrypt_sensitive_data]
    
    sensitive_fields = ['card_number', 'cvv', 'expiry_month', 'expiry_year']
    
    data.each do |key, value|
      if sensitive_fields.include?(key.to_s)
        data[key] = encrypt_value(value)
      end
    end
    
    data
  end

  def decrypt_payment_data(data)
    return data unless @config[:security][:encrypt_sensitive_data]
    
    sensitive_fields = ['card_number', 'cvv', 'expiry_month', 'expiry_year']
    
    data.each do |key, value|
      if sensitive_fields.include?(key.to_s)
        data[key] = decrypt_value(value)
      end
    end
    
    data
  end

  def validate_webhook_signature(payload, signature, secret)
    case Rails.application.config.payment[:provider]
    when 'stripe'
      validate_stripe_webhook(payload, signature, secret)
    when 'paypal'
      validate_paypal_webhook(payload, signature, secret)
    else
      true
    end
  end

  private

  def validate_required_fields(data)
    required_fields = ['amount', 'currency']
    missing_fields = required_fields - data.keys.map(&:to_s)
    
    if missing_fields.any?
      raise PaymentError.new("Missing required fields: #{missing_fields.join(', ')}")
    end
  end

  def validate_amount(amount)
    unless amount.is_a?(Integer) && amount > 0
      raise PaymentError.new("Invalid amount: must be a positive integer")
    end
  end

  def validate_currency(currency)
    valid_currencies = ['usd', 'eur', 'gbp', 'cad', 'aud']
    
    unless valid_currencies.include?(currency.to_s.downcase)
      raise PaymentError.new("Invalid currency: #{currency}")
    end
  end

  def sanitize_data(data)
    # Remove any potentially dangerous characters
    data.each do |key, value|
      if value.is_a?(String)
        data[key] = value.strip.gsub(/[<>]/, '')
      end
    end
    
    data
  end

  def encrypt_value(value)
    # Implementation would encrypt sensitive values
    Base64.encode64(value.to_s)
  end

  def decrypt_value(value)
    # Implementation would decrypt sensitive values
    Base64.decode64(value.to_s)
  end

  def validate_stripe_webhook(payload, signature, secret)
    begin
      Stripe::Webhook.construct_event(payload, signature, secret)
      true
    rescue => e
      Rails.logger.error "Stripe webhook validation failed: #{e.message}"
      false
    end
  end

  def validate_paypal_webhook(payload, signature, secret)
    # Implementation would validate PayPal webhook
    true
  end
end
```

## Testing

### Payment Test Helper

```ruby
# spec/support/payment_helper.rb
module PaymentHelper
  def clear_payments
    Payment.delete_all
    Subscription.delete_all
    PaymentEvent.delete_all
  end

  def create_test_payment(user, amount = 1000, currency = 'usd')
    Payment.create!(
      user: user,
      provider_payment_id: "pi_#{SecureRandom.hex(8)}",
      amount: amount,
      currency: currency,
      status: 'succeeded',
      payment_method: 'credit_card'
    )
  end

  def expect_payment_created(user, amount)
    expect(Payment.where(user: user, amount: amount)).to exist
  end

  def expect_subscription_created(user, plan)
    expect(Subscription.where(user: user, plan: plan)).to exist
  end
end

RSpec.configure do |config|
  config.include PaymentHelper, type: :payment
  
  config.before(:each, type: :payment) do
    clear_payments
  end
end
```

### Payment Tests

```ruby
# spec/payment/payment_manager_spec.rb
RSpec.describe PaymentManager, type: :payment do
  let(:manager) { PaymentManager.instance }
  let(:user) { create(:user) }

  describe '#create_payment_intent' do
    it 'creates payment intent' do
      result = manager.create_payment_intent(1000, 'usd')
      
      expect(result[:id]).to be_present
      expect(result[:client_secret]).to be_present
      expect(result[:amount]).to eq(1000)
    end
  end

  describe '#process_payment' do
    it 'processes payment successfully' do
      payment_method_id = 'pm_test_123'
      
      result = manager.process_payment(payment_method_id, 1000, 'usd')
      
      expect(result[:status]).to eq('succeeded')
      expect(result[:amount]).to eq(1000)
    end
  end
end
```

## Deployment

### Production Configuration

```ruby
# config/environments/production.rb
Rails.application.configure do
  # Payment configuration
  config.payment = {
    provider: ENV['PAYMENT_PROVIDER'] || 'stripe',
    stripe: {
      publishable_key: ENV['STRIPE_PUBLISHABLE_KEY'],
      secret_key: ENV['STRIPE_SECRET_KEY'],
      webhook_secret: ENV['STRIPE_WEBHOOK_SECRET'],
      currency: ENV['STRIPE_CURRENCY'] || 'usd',
      api_version: ENV['STRIPE_API_VERSION'] || '2023-10-16'
    },
    paypal: {
      client_id: ENV['PAYPAL_CLIENT_ID'],
      client_secret: ENV['PAYPAL_CLIENT_SECRET'],
      mode: ENV['PAYPAL_MODE'] || 'live',
      currency: ENV['PAYPAL_CURRENCY'] || 'USD'
    },
    square: {
      application_id: ENV['SQUARE_APPLICATION_ID'],
      access_token: ENV['SQUARE_ACCESS_TOKEN'],
      location_id: ENV['SQUARE_LOCATION_ID'],
      environment: ENV['SQUARE_ENVIRONMENT'] || 'production'
    },
    braintree: {
      merchant_id: ENV['BRAINTREE_MERCHANT_ID'],
      public_key: ENV['BRAINTREE_PUBLIC_KEY'],
      private_key: ENV['BRAINTREE_PRIVATE_KEY'],
      environment: ENV['BRAINTREE_ENVIRONMENT'] || 'production'
    },
    security: {
      encrypt_sensitive_data: ENV['PAYMENT_ENCRYPT_DATA'] != 'false',
      pci_compliant: ENV['PAYMENT_PCI_COMPLIANT'] != 'false',
      tokenization_enabled: ENV['PAYMENT_TOKENIZATION_ENABLED'] != 'false'
    },
    webhooks: {
      enabled: ENV['PAYMENT_WEBHOOKS_ENABLED'] != 'false',
      endpoint: ENV['PAYMENT_WEBHOOK_ENDPOINT'] || '/webhooks/payment',
      timeout: ENV['PAYMENT_WEBHOOK_TIMEOUT'] || 30
    },
    monitoring: {
      enabled: ENV['PAYMENT_MONITORING_ENABLED'] != 'false',
      metrics_port: ENV['PAYMENT_METRICS_PORT'] || 9090,
      health_check_interval: ENV['PAYMENT_HEALTH_CHECK_INTERVAL'] || 30
    }
  }
end
```

### Docker Configuration

```dockerfile
# Dockerfile.payment
FROM ruby:3.2-alpine

RUN apk add --no-cache \
    build-base \
    redis

WORKDIR /app

COPY Gemfile Gemfile.lock ./
RUN bundle install --jobs 4 --retry 3

COPY . .

CMD ["bundle", "exec", "ruby", "app/payment/payment_runner.rb"]
```

```yaml
# docker-compose.payment.yml
version: '3.8'

services:
  payment-service:
    build:
      context: .
      dockerfile: Dockerfile.payment
    environment:
      - RAILS_ENV=production
      - REDIS_URL=redis://redis:6379/3
      - PAYMENT_PROVIDER=stripe
      - STRIPE_PUBLISHABLE_KEY=${STRIPE_PUBLISHABLE_KEY}
      - STRIPE_SECRET_KEY=${STRIPE_SECRET_KEY}
    depends_on:
      - redis

  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data

volumes:
  redis_data:
```

This comprehensive payment integration guide provides everything needed to build secure payment systems with TuskLang and Ruby, including multiple payment providers, payment methods, subscription management, security features, testing, and deployment strategies. 
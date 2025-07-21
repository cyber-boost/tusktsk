#!/usr/bin/env ruby
require 'matrix'
require 'json'
require 'time'

class OrbitalMechanics
  def initialize
    @earth_radius = 6371000
    @earth_mu = 3.986e14
    @j2 = 1.08263e-3
  end
  
  def kepler_to_cartesian(a, e, i, omega, w, nu)
    p = a * (1 - e**2)
    r = p / (1 + e * Math.cos(nu))
    x_orb = r * Math.cos(nu)
    y_orb = r * Math.sin(nu)
    
    cos_omega, sin_omega = Math.cos(omega), Math.sin(omega)
    cos_w, sin_w = Math.cos(w), Math.sin(w)
    cos_i, sin_i = Math.cos(i), Math.sin(i)
    
    x = x_orb * (cos_omega * cos_w - sin_omega * sin_w * cos_i) - 
        y_orb * (cos_omega * sin_w + sin_omega * cos_w * cos_i)
    y = x_orb * (sin_omega * cos_w + cos_omega * sin_w * cos_i) - 
        y_orb * (sin_omega * sin_w - cos_omega * cos_w * cos_i)
    z = x_orb * sin_w * sin_i + y_orb * cos_w * sin_i
    
    [x, y, z]
  end
  
  def orbital_period(semi_major_axis)
    2 * Math::PI * Math.sqrt(semi_major_axis**3 / @earth_mu)
  end
  
  def hohmann_transfer(r1, r2)
    a_transfer = (r1 + r2) / 2
    v1_circular = Math.sqrt(@earth_mu / r1)
    v2_circular = Math.sqrt(@earth_mu / r2)
    v1_transfer = Math.sqrt(@earth_mu * (2/r1 - 1/a_transfer))
    v2_transfer = Math.sqrt(@earth_mu * (2/r2 - 1/a_transfer))
    
    delta_v1 = v1_transfer - v1_circular
    delta_v2 = v2_circular - v2_transfer
    transfer_time = Math::PI * Math.sqrt(a_transfer**3 / @earth_mu)
    
    {
      delta_v1: delta_v1,
      delta_v2: delta_v2,
      transfer_time: transfer_time,
      total_delta_v: delta_v1.abs + delta_v2.abs
    }
  end
end

class SatelliteSubsystems
  def initialize
    @power_system = {
      solar_panels: 4,
      battery_capacity: 100,
      current_charge: 100
    }
    @thermal_system = {
      heaters: 2,
      radiators: 4,
      temperature: 20
    }
    @propulsion = {
      fuel_mass: 50,
      specific_impulse: 300,
      thrust: 10
    }
    @communication = {
      antennas: 3,
      transmit_power: 20,
      frequency: 2.4e9
    }
  end
  
  def power_management(solar_irradiance, power_consumption)
    power_generated = @power_system[:solar_panels] * solar_irradiance * 0.2
    net_power = power_generated - power_consumption
    
    if net_power > 0
      @power_system[:current_charge] = [
        [@power_system[:current_charge] + net_power * 0.1, @power_system[:battery_capacity]].min,
        0
      ].max
    else
      @power_system[:current_charge] = [
        [@power_system[:current_charge] + net_power * 0.1, @power_system[:battery_capacity]].min,
        0
      ].max
    end
    
    {
      generated: power_generated,
      consumed: power_consumption,
      battery_level: @power_system[:current_charge],
      status: @power_system[:current_charge] > 20 ? 'nominal' : 'critical'
    }
  end
  
  def thermal_control(external_temp, internal_heat)
    target_temp = 20
    temp_diff = external_temp - @thermal_system[:temperature]
    heat_transfer = temp_diff * 0.1 + internal_heat * 0.05
    
    if @thermal_system[:temperature] > target_temp + 10
      @thermal_system[:radiators] = 4
      heat_transfer -= 2
    elsif @thermal_system[:temperature] < target_temp - 10
      @thermal_system[:heaters] = 2
      heat_transfer += 1.5
    end
    
    @thermal_system[:temperature] += heat_transfer * 0.1
    
    {
      temperature: @thermal_system[:temperature],
      heaters_active: @thermal_system[:heaters],
      radiators_active: @thermal_system[:radiators],
      status: (@thermal_system[:temperature] - target_temp).abs < 15 ? 'nominal' : 'warning'
    }
  end
  
  def propulsion_maneuver(delta_v_required)
    return { success: false, reason: 'no_fuel' } if @propulsion[:fuel_mass] <= 0
    
    mass_ratio = Math.exp(delta_v_required / (@propulsion[:specific_impulse] * 9.81))
    fuel_needed = @propulsion[:fuel_mass] * (1 - 1/mass_ratio)
    
    if fuel_needed > @propulsion[:fuel_mass]
      return {
        success: false,
        reason: 'insufficient_fuel',
        fuel_needed: fuel_needed,
        fuel_available: @propulsion[:fuel_mass]
      }
    end
    
    @propulsion[:fuel_mass] -= fuel_needed
    burn_time = fuel_needed / @propulsion[:thrust] * @propulsion[:specific_impulse]
    
    {
      success: true,
      fuel_consumed: fuel_needed,
      burn_time: burn_time,
      remaining_fuel: @propulsion[:fuel_mass]
    }
  end
end

class SpaceFramework
  attr_reader :orbital_mechanics, :subsystems
  
  def initialize
    @orbital_mechanics = OrbitalMechanics.new
    @subsystems = SatelliteSubsystems.new
  end
  
  def complete_mission_cycle(satellite_orbit, mission_duration_hours = 24)
    results = {}
    
    # Calculate orbital position
    orbit_data = @orbital_mechanics.kepler_to_cartesian(
      satellite_orbit[:a], satellite_orbit[:e], satellite_orbit[:i],
      satellite_orbit[:omega], satellite_orbit[:w], satellite_orbit[:nu]
    )
    
    # Simulate power and thermal systems
    power_status = @subsystems.power_management(1361, 25)
    thermal_status = @subsystems.thermal_control(-200, 15)
    
    # Calculate orbital period
    orbital_period = @orbital_mechanics.orbital_period(satellite_orbit[:a])
    
    # Plan Hohmann transfer (example)
    transfer_data = @orbital_mechanics.hohmann_transfer(
      satellite_orbit[:a], satellite_orbit[:a] * 1.5
    )
    
    results[:orbit_position] = orbit_data
    results[:power_status] = power_status
    results[:thermal_status] = thermal_status
    results[:orbital_period] = orbital_period
    results[:transfer_analysis] = transfer_data
    results[:mission_success] = power_status[:status] == 'nominal' && 
                                thermal_status[:status] == 'nominal'
    
    results
  end
  
  def emergency_procedures(emergency_type)
    case emergency_type
    when 'power_critical'
      {
        action: 'enter_safe_mode',
        subsystems_shutdown: ['payload', 'communication'],
        battery_conservation: true,
        recovery_time: 4
      }
    when 'thermal_emergency'
      {
        action: 'thermal_protection',
        radiators_max: true,
        payload_shutdown: true,
        attitude_control: 'sun_pointing'
      }
    when 'communication_loss'
      {
        action: 'autonomous_mode',
        beacon_transmission: true,
        ground_contact_attempts: 3,
        emergency_frequency: true
      }
    else
      {
        action: 'unknown_emergency',
        default_response: 'safe_mode'
      }
    end
  end
end

puts "G23 SPACE DONE!" if __FILE__ == $0 
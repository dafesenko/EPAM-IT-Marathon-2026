# Sequential: one waiter serves one location at a time, so these scenarios cannot share a location waiter in parallel.
@ui @group-pre-order @level-2 @sequential
Feature: Submitting a group pre-order from the owner's page

  The owner sends the group's pre-order from the group page. The button is there while the group is open, and it can
  only be pressed once a guest has confirmed a cart.

  Background:
    Given a restaurant location with a table for 4 guests
    And a main course priced 180 on its menu

  Rule: The owner sends the confirmed pre-order from the page

    @smoke
    Scenario: The owner sends the pre-order from the group page
      Given Olena has booked the table for 4 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Olena has started a pre-order with 1 main course
      And Andrii has added 2 main courses to the order
      And Andrii has confirmed the order
      And Maria has added 1 main course to the order
      When Olena opens the group order page
      And Olena submits the pre-order from the page
      Then the page shows the pre-order as sent to the restaurant

  Rule: Nothing can be sent until a guest has confirmed a cart

    Scenario: The submit button is disabled while no cart is confirmed
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Andrii has added 1 main course to the order
      When Olena opens the group order page
      Then the submit button is disabled
      And the page still collects pre-orders

  Rule: Backing out of the dialog sends nothing

    Scenario: The owner cancels the submit dialog
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Olena has started a pre-order with 1 main course
      And Andrii has added 1 main course to the order
      And Andrii has confirmed the order
      When Olena opens the group order page
      And Olena opens the submit dialog and cancels
      Then the page still collects pre-orders

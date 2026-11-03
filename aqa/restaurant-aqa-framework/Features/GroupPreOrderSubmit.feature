# Sequential: one waiter serves one location at a time, so these scenarios cannot share a location waiter in parallel.
@api @group-pre-order @level-2 @sequential
Feature: Submitting a group pre-order

  The owner confirms the group's pre-order. The dishes of the guests who confirmed their carts go into the
  reservation's pre-order, the group is closed for changes, and the waiter sees what was ordered.

  Background:
    Given a restaurant location with a table for 4 guests
    And a main course priced 180 on its menu

  Rule: Only the confirmed carts are passed on to the pre-order

    @smoke
    Scenario: The restaurant receives the confirmed dishes merged by dish
      Given Olena has booked the table for 4 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Olena has invited Carmen
      And Olena has started a pre-order with 1 main course
      And Andrii has added 2 main courses to the order
      And Andrii has confirmed the order
      And Maria has added 1 main course to the order
      And Maria has confirmed the order
      And Carmen has added 3 main courses to the order
      When Olena submits the group pre-order
      Then the restaurant receives 3 main courses

    Scenario: The group is submitted once the pre-order is passed on
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Olena has started a pre-order with 1 main course
      And Andrii has added 1 main course to the order
      And Andrii has confirmed the order
      When Olena submits the group pre-order
      And Olena checks the group status
      Then the group is submitted

    Scenario: The waiter sees the owner's dishes and the confirmed guest dishes on the order
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Olena has started a pre-order with 1 main course
      And Andrii has added 2 main courses to the order
      And Andrii has confirmed the order
      And Maria has added 1 main course to the order
      And Maria has confirmed the order
      When Olena submits the group pre-order
      And the waiter views the order of Olena
      Then the waiter sees 4 main courses on the order
      And the order is marked as submitted by the customer

  Rule: Nothing is passed on when no guest has confirmed an order

    Scenario: The submission is refused when every guest's cart is still unconfirmed
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Olena has started a pre-order with 1 main course
      And Andrii has added 1 main course to the order
      When Olena submits the group pre-order
      Then the visitor is told "No submitted guest sessions found for the group reservation."

  Rule: The submission needs a pre-order to pass the dishes on to

    Scenario: The submission is refused when the owner has not started a pre-order
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Andrii has added 1 main course to the order
      And Andrii has confirmed the order
      When Olena submits the group pre-order
      Then the visitor is told the pre-order is not found

  Rule: A group that does not exist cannot be submitted

    Scenario: The submission is refused for a group that does not exist
      Given Olena has booked the table for 2 guests
      And Olena has started a pre-order with 1 main course
      When Olena submits a group pre-order that does not exist
      Then the group could not be found

  Rule: Only the owner of the group can submit it

    Scenario: Another customer cannot submit the group pre-order
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Olena has started a pre-order with 1 main course
      And Andrii has added 1 main course to the order
      And Andrii has confirmed the order
      And Mark has signed up
      When Mark tries to submit the group pre-order of Olena's group
      Then the visitor is told "Only the reservation owner can submit the group pre-order."

  Rule: A group pre-order is submitted only once

    Scenario: A second submission of the same group is refused
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Olena has started a pre-order with 1 main course
      And Andrii has added 1 main course to the order
      And Andrii has confirmed the order
      When Olena submits the group pre-order
      And Olena submits the group pre-order
      Then the visitor is told "Group reservation was submitted."

  Rule: The owner can add dishes to their own part of the group

    # Known defect (expected to fail until fixed): the owner's session in the group has no token, so the owner cannot
    # add a dish to it. The backend answers with an internal error instead of adding the dish.
    @known-defect
    Scenario: The owner adds a dish to their own group cart
      Given Olena has booked the table for 2 guests
      When Olena adds 1 main course to their own group cart
      Then the dish is added to the owner's cart

# Sequential: one waiter serves one location at a time, so these scenarios cannot share a location waiter in parallel.
@api @group-pre-order @level-3 @sequential
Feature: Removing a guest from a group pre-order

  The owner can take a guest out of the group. The list and the totals change at once, and the removed guest's
  dishes do not reach the restaurant. Only the owner can do this, and the owner stays in their own group.

  Background:
    Given a restaurant location with a table for 4 guests
    And a main course priced 180 on its menu

  Rule: The owner removes a guest and sees the updated list and totals

    @smoke
    Scenario: Removing one of several guests recalculates the group total
      Given Olena has booked the table for 4 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Olena has invited Carmen
      And Andrii has added 2 main courses to the order
      And Maria has added 1 main course to the order
      When Olena removes Andrii from the group
      And Olena views the group's participants
      Then the participants are:
        | name   | confirmed | total |
        | Owner  | no        | 0     |
        | Maria  | no        | 180   |
        | Carmen | no        | 0     |
      And the group total is 180

    Scenario: A guest who had confirmed their order is removed together with that order
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Andrii has added 2 main courses to the order
      And Andrii has confirmed the order
      And Maria has added 1 main course to the order
      When Olena removes Andrii from the group
      And Olena views the group's participants
      Then the participants are:
        | name   | confirmed | total |
        | Owner  | no        | 0     |
        | Maria  | no        | 180   |
      And the group total is 180

    Scenario: Removing the last guest leaves only the owner
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Andrii has added 1 main course to the order
      When Olena removes Andrii from the group
      And Olena views the group's participants
      Then the participants are:
        | name  | confirmed | total |
        | Owner | no        | 0     |
      And the group total is 0

  Rule: A guest can only be removed from the group they belong to

    Scenario: A guest of another group cannot be removed from this group
      Given Olena has booked the table for 2 guests
      And another table for 2 guests
      And Mark has booked the table for 2 guests
      And Mark has invited Carmen
      When Olena tries to remove Carmen from the group
      Then the visitor is told "Guest session does not belong to this group reservation."

    Scenario: A guest who does not exist cannot be removed
      Given Olena has booked the table for 2 guests
      When Olena tries to remove a guest who does not exist
      Then the visitor is told the guest could not be found

  Rule: The owner stays in their own group

    Scenario: The owner's own session cannot be removed
      Given Olena has booked the table for 2 guests
      When Olena tries to remove the owner from the group
      Then the visitor is told "The owner session cannot be removed from the group."

  Rule: Only the owner of the group can remove a guest

    Scenario: Another customer cannot remove a guest
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Mark has signed up
      When Mark tries to remove Andrii from Olena's group
      Then the visitor is told "Only the reservation owner can perform this action."

    Scenario: A guest cannot remove another guest
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      When Andrii tries to remove Maria from Olena's group
      Then the visitor is asked to sign in

  Rule: A removed guest's dishes do not reach the restaurant

    Scenario: The waiter's order leaves out a guest who was removed before the submission
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
      And Carmen has confirmed the order
      When Olena removes Carmen from the group
      And Olena submits the group pre-order
      And the waiter views the order of Olena
      Then the waiter sees 4 main courses on the order

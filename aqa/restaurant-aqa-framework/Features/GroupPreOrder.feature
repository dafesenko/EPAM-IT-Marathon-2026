# Sequential: one waiter serves one location at a time, so these scenarios cannot share a location waiter in parallel.
@api @group-pre-order @level-1 @sequential
Feature: Viewing a group pre-order

  The owner of a group booking sees who has joined, what each guest has ordered, and what each of them owes.
  Nobody else sees that list.

  Background:
    Given a restaurant location with a table for 4 guests
    And a main course priced 180 on its menu

  Rule: The owner sees every participant with their dishes and totals

    @smoke
    Scenario: The owner sees what every guest has ordered
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Andrii has added 2 main courses to the order
      And Maria has added 1 main course to the order
      When Olena views the group's participants
      Then the participants are:
        | name   | confirmed | total |
        | Owner  | no        | 0     |
        | Andrii | no        | 360   |
        | Maria  | no        | 180   |
      And the group total is 540

  Rule: The total follows the guests' carts

    Scenario: The group total changes when a guest adds more to the cart
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      And Andrii has added 1 main course to the order
      When Andrii adds 1 more main course
      And Olena views the group's participants
      Then the group total is 360

  Rule: A participant who has not confirmed the order is marked as such

    Scenario: Confirmed and unconfirmed orders are shown side by side
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Andrii has added 1 main course to the order
      And Andrii has confirmed the order
      And Maria has added 1 main course to the order
      When Olena views the group's participants
      Then the participants are:
        | name   | confirmed | total |
        | Owner  | no        | 0     |
        | Andrii | yes       | 180   |
        | Maria  | no        | 180   |

  Rule: A participant without dishes is still listed

    Scenario: A guest who has not ordered anything appears with a zero total
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      When Olena views the group's participants
      Then the participants are:
        | name   | confirmed | total |
        | Owner  | no        | 0     |
        | Andrii | no        | 0     |

  Rule: A group nobody has joined lists only its owner

    Scenario: The owner is the only participant of a new group
      Given Olena has booked the table for 2 guests
      When Olena views the group's participants
      Then the participants are:
        | name  | confirmed | total |
        | Owner | no        | 0     |
      And the group total is 0

  Rule: Only the owner of the group sees the participants

    Scenario: Another customer is not allowed to see the participants
      Given Olena has booked the table for 2 guests
      And Mark has signed up
      When Mark views the participants of Olena's group
      Then the visitor is not allowed to see the participants

    Scenario: A guest is asked to sign in to see the participants
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      When Andrii views the participants of Olena's group
      Then the visitor is asked to sign in

    Scenario: The waiter is not allowed to see the participants
      Given Olena has booked the table for 2 guests
      When the waiter views the participants of Olena's group
      Then the visitor is not allowed to see the participants

    Scenario: Someone who is not signed in is asked to sign in
      Given Olena has booked the table for 2 guests
      When someone who is not signed in views the participants of Olena's group
      Then the visitor is asked to sign in

  Rule: A group that does not exist cannot be viewed

    Scenario: Asking for the participants of an unknown group
      Given Olena has signed up
      When Olena views the participants of a group that does not exist
      Then the group could not be found

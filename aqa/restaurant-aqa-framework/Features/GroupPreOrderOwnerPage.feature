# Sequential: one waiter serves one location at a time, so these scenarios cannot share a location waiter in parallel.
@ui @group-pre-order @level-1 @sequential
Feature: The owner's group pre-order page

  The owner opens the group pre-order page of a booking and sees a card for every participant: their dishes, whether
  they confirmed their order, and what they owe. The page is the same whether there are guests or not.

  Background:
    Given a restaurant location with a table for 4 guests
    And a main course priced 180 on its menu

  Rule: The owner sees every participant's card with the subtotal

    @smoke
    Scenario: The owner sees a card for each participant with what they ordered
      Given Olena has booked the table for 4 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Andrii has added 2 main courses to the order
      And Maria has added 1 main course to the order
      When Olena opens the group order page
      Then the participant cards are:
        | participant | confirmed | subtotal |
        | Owner       | no        | 0        |
        | Andrii      | no        | 360      |
        | Maria       | no        | 180      |

  Rule: A confirmed order is shown as submitted

    Scenario: A guest who confirmed the order is shown as submitted
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Andrii has added 1 main course to the order
      And Andrii has confirmed the order
      When Olena opens the group order page
      Then the participant cards are:
        | participant | confirmed | subtotal |
        | Owner       | no        | 0        |
        | Andrii      | yes       | 180      |

  Rule: A group nobody has joined shows only the owner's card

    Scenario: The owner's card is the only one on a new group
      Given Olena has booked the table for 2 guests
      When Olena opens the group order page
      Then the participant cards are:
        | participant | confirmed | subtotal |
        | Owner       | no        | 0        |

  Rule: The owner can open the page of a full group

    # Known defect (expected to fail until fixed): the page reads the group's invitation first, and the backend
    # refuses that read once every seat is taken, so the owner gets an error instead of the participants list.
    @known-defect
    Scenario: The owner sees the cards of a group whose every seat is taken
      Given Olena has booked the table for 2 guests
      And Olena has invited Andrii
      When Olena opens the group order page
      Then the participant cards are:
        | participant | confirmed | subtotal |
        | Owner       | no        | 0        |
        | Andrii      | no        | 0        |

# Sequential: one waiter serves one location at a time, so these scenarios cannot share a location waiter in parallel.
@ui @group-pre-order @level-3 @sequential
Feature: Removing a guest from the owner's group page

  On the group page, the owner removes a guest with the guest's own "Remove Guest" button and a confirmation. The guest's
  card disappears; backing out of the confirmation keeps the guest.

  Background:
    Given a restaurant location with a table for 4 guests
    And a main course priced 180 on its menu

  Rule: The owner removes a guest from the page

    @smoke
    Scenario: The owner removes a guest and the card disappears
      Given Olena has booked the table for 4 guests
      And Olena has invited Andrii
      And Olena has invited Maria
      And Andrii has added 1 main course to the order
      When Olena opens the group order page
      And Olena removes Andrii from the page
      Then the participant cards are:
        | participant | confirmed | subtotal |
        | Owner       | no        | 0        |
        | Maria       | no        | 0        |

  Rule: Backing out of the confirmation keeps the guest

    Scenario: The owner cancels the removal and the guest stays
      Given Olena has booked the table for 3 guests
      And Olena has invited Andrii
      And Andrii has added 1 main course to the order
      When Olena opens the group order page
      And Olena opens the remove dialog for Andrii and cancels
      Then the participant cards are:
        | participant | confirmed | subtotal |
        | Owner       | no        | 0        |
        | Andrii      | no        | 180      |

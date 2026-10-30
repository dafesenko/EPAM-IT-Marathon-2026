terraform {
  required_version = ">= 1.10"

  backend "s3" {
    # Replace <AWS_ACCOUNT_ID> with your AWS account ID.
    # Backend blocks do not support variables, so this value must be edited by hand.
    bucket       = "restaurant-terraform-state-<AWS_ACCOUNT_ID>"
    key          = "restaurant/terraform.tfstate"
    region       = "eu-north-1"
    use_lockfile = true
    encrypt      = true
  }
}

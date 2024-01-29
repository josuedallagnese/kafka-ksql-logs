#!/usr/bin/python3

import random
import json
import string

def generate_random_name():
    return ''.join(random.choice(string.ascii_letters + ' ') for i in range(100))[:random.randint(1, 100)]

def generate_random_email():
    username = ''.join(random.choices(string.ascii_letters + string.digits, k=8))

    domains = ['gmail.com', 'yahoo.com', 'hotmail.com', 'example.com']
    domain = random.choice(domains)
    email = f'{username}@{domain}'
    return email

def get_payload():
    name = generate_random_name()
    mail = generate_random_email()

    return json.dumps({"name" : name, "mail" : mail })

def generate_accounts(number_of_regs):
    with open("user-files/resources/accounts.tsv", "w") as f:
        f.write("payload\n")
        for _ in range(number_of_regs):
            f.write(f"{get_payload()}\n")

generate_accounts(100_000)
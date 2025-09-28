Azure Serverless E-Commerce Platform
====================================

Project Demo: https://drive.google.com/file/d/1rD-rHRhIeWShbxSsXZ0rUD_H2-vMfMT0/view?usp=drive_link

This repository contains the source code and architecture for a modern, scalable, and resilient e-commerce platform built entirely on serverless and integration services in Microsoft Azure. The solution is designed as a collection of decoupled microservices that communicate asynchronously, providing significant advantages in scalability, maintainability, and fault tolerance.

📖 Overview
-----------

This project serves as a reference implementation for building event-driven systems in the cloud. It demonstrates how to handle a complex business process—like an e-commerce order—in a way that is both highly responsive to the user and resilient to backend failures.

### Core Architectural Principles

*   **Decoupling:** Each component (Inventory, Payment, Notifications) is a self-contained microservice. This allows for individual services to be updated, deployed, or scaled without impacting the rest of the system.
    
*   **Asynchronous Communication:** User requests are acknowledged immediately, while long-running processes are handled in the background. This leads to a fast and responsive user experience, even during peak load.
    
*   **Serverless & Elastic:** The platform automatically scales resources based on demand. This means you only pay for what you use, eliminating the need for capacity planning and over-provisioning of servers.
    
*   **Resilience & Fault Tolerance:** The use of queues ensures that if a downstream service is unavailable, the process can resume later without data loss. This guarantees that business processes will eventually complete.
    

🛠️ Technology Choices & Rationale
----------------------------------

### 🏛️ API Gateway: Azure API Management (`apim-ecommerce-prod`)

> **Role:** Acts as the single, secure front door for all client applications.
> 
> **Why:** It decouples clients from backend services and handles cross-cutting concerns like authentication (JWT validation), rate limiting, caching, and request transformation in a centralized place.

### 🔗 Workflow Orchestration: Azure Logic Apps

> **Role:** Visually designs and automates business processes, such as the initial order intake (`OrderIntake-LA`) and notifications (`la-notificationhandler-prod`).
> 
> **Why:** Chosen for its powerful connectors and low-code designer, making it incredibly fast to build and visualize stateful workflows.

### ⚙️ Business Logic: Azure Functions

> **Role:** Executes small, single-purpose pieces of business logic. Each core business domain (e.g., Payments, Inventory) has its own dedicated Function App (`func-payments-prod`, `func-inventory-prod`).
> 
> **Why:** Provides a cost-effective, event-driven, and scalable way to run custom code. Separating Function Apps by domain reinforces the microservice boundaries.

### 📬 Reliable Messaging: Azure Service Bus (`sb-ecommerce-prod`)

> **Role:** Manages the sequence of operations in a workflow using queues.
> 
> **Why:** Guarantees message delivery (`at-least-once`) and supports advanced features like message ordering and dead-lettering. This ensures no step in the order process is ever lost.

### 📣 Event Broadcasting: Azure Event Grid (`egt-orderevents-prod`)

> **Role:** Decouples services by allowing them to broadcast and subscribe to important business events (e.g., `OrderShipped`).
> 
> **Why:** A highly scalable publish-subscribe service that enables reactive, event-driven architectures and allows parallel workflows (like notifications) to operate independently.

### 🗄️ Primary Database: Azure Cosmos DB for NoSQL (`cosmos-ecommerce-prod`)

> **Role:** Stores all business data, including product catalogs, customer information, and order histories.
> 
> **Why:** A globally-distributed database offering single-digit millisecond latency and limitless scale. Its schema-agnostic nature is perfect for evolving e-commerce data models.

🔄 Core Transaction Workflow
----------------------------

1.  **Order Placement:** The client sends the cart details to the `POST /orders` endpoint on API Management.
    
2.  **Immediate Intake:** A Logic App (`OrderIntake-LA`) receives the request.
    
    *   It immediately saves the order to Cosmos DB with a `Pending` status.
        
    *   It places a message with the `orderId` onto the `new-orders` Service Bus queue.
        
    *   It returns a `202 Accepted` response to the client, completing the synchronous interaction in milliseconds.
        
3.  **Inventory Check (Async):** An Azure Function (`CheckInventory-Func` within `func-inventory-prod`) is triggered. It verifies stock, decrements the count in Cosmos DB, and places a message on the `process-payment` queue.
    
4.  **Payment Processing (Async):** A second Function (`ProcessPayment-Func` within `func-payments-prod`) is triggered. It communicates with a payment gateway, updates the order status to `Paid`, and places a message on the `prepare-shipment` queue. It also publishes an `OrderPaid` event to Event Grid.
    
5.  **Shipment & Fulfillment (Async):** A third Function (`CreateShipment-Func` within `func-shipping-prod`) is triggered. It generates a shipping label, updates the order with a tracking number, and publishes an `OrderShipped` event.
    
6.  **Customer Notifications (Parallel):** A separate Logic App (`la-notificationhandler-prod`) listens for events from Event Grid and sends the appropriate email or SMS to the customer.
    

🏗️ Infrastructure & Resource Group Structure
---------------------------------------------

Resources are segregated into shared and service-specific Resource Groups to enforce clear ownership and microservice boundaries.

Resource Group Name

Purpose

Azure Resources Contained

`rg-shared-prod`

Central infrastructure for the platform.

• Azure API Management: `apim-ecommerce-prod` • Azure Cosmos DB Account: `cosmos-ecommerce-prod` • Azure Service Bus Namespace: `sb-ecommerce-prod` • Azure Event Grid Topic: `egt-orderevents-prod` • Azure Storage: `stmedia-prod`

`rg-payments-prod`

All resources for the Payment Service.

• Azure Function App: `func-payments-prod` • Application Insights: `appi-payments-prod` • Azure Key Vault: `kv-payments-prod`

`rg-inventory-prod`

All resources for the Inventory Service.

• Azure Function App: `func-inventory-prod` • Application Insights: `appi-inventory-prod`

`rg-shipping-prod`

All resources for the Shipping Service.

• Azure Function App: `func-shipping-prod` • Application Insights: `appi-shipping-prod`

`rg-notifications-prod`

All resources for the Notification Service.

• Azure Logic App: `la-notificationhandler-prod`

🚀 Future Enhancements
----------------------------------------

*   \[ \] **Centralized Logging & Monitoring:** Integrate all services with a unified **Azure Application Insights** workspace to enable distributed tracing and end-to-end observability of the order flow.
    

*   \[ \] **Order Management & Customer Service Portal:** Build a simple web interface (e.g., on Azure Static Web Apps) for internal teams to view order history, check statuses, and handle customer service requests by calling secure APIs exposed through APIM.
    
*   \[ \] **Persistent Shopping Cart Service:** Develop a dedicated microservice with its own set of functions (`GetCart`, `AddToCart`) and a `carts` container in Cosmos DB to manage user shopping carts.
    
*   \[ \] **Advanced Inventory Management:** Evolve the inventory logic to support stock reservation. When an order is placed, "reserve" the stock. Only fully remove it after shipment, and release the reservation if payment fails.

*   \[ \] **Real Payment Gateway Integration:** Replace the simulated payment function with a real integration to a provider like **Stripe** or **PayPal**, including secure handling of API keys and processing of webhooks.
    
*   \[ \] **Real Shipping Provider Integration:** Integrate with a service like **ShipStation** or carrier APIs (FedEx, UPS) to fetch real-time shipping rates and generate printable labels.
    
*   \[ \] **Advanced Product Search:** Integrate **Azure AI Search** to provide a powerful, fast, and feature-rich search experience (filtering, faceting, suggestions) over the product catalog.

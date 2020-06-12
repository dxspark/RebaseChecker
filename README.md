# Rebase Checker
Pull Request Rebase Checker Policy Service for Azure Devops on Azure Function

We created Rebase Checker to validate the state of the source branch versus the target branch in a Pull Request on Azure Devops.

![Project settings](images/screenshot13.png)

![Project settings](images/screenshot12.png)

## Setup

1. Deploy your Rebase Checker to Azure Functions

2. On a Azure Devops project, select **Project settings**:  
![Project settings](images/screenshot01.png)

3. Select **Service hooks**:  
![Service hooks](images/screenshot02.png)

4. Add a new service hook subscription: **Web Hooks**:  
![Web Hooks](images/screenshot03.png)

5. Select trigger **Pull request created**:  
![Pull request created](images/screenshot04.png)

6. Add your **Rebase Checker Azure Function URL**:  
![Azure Function URL](images/screenshot05.png)

7. Now add another trigger **Pull request updated**:  
![Pull request updated](images/screenshot06.png)

8. Add your **Rebase Checker Azure Function URL**:  
![Azure Function URL](images/screenshot05.png)

9. On project settings. select **Repositories**:  
![Repositories](images/screenshot07.png)

9. Select the **repository**:  
![Repository](images/screenshot08.png)

10. Select **Policies**:  
![Policies](images/screenshot09.png)

11. Select **Add status policy**:  
![Add status policy](images/screenshot10.png)

12. In **Status to check** add **checker/rebasechecker**:  
![Status to check](images/screenshot11.png)
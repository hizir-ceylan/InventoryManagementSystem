import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { deviceApi, networkApi, changeLogApi, statisticsApi } from '../api'
import type { Device, NetworkScanRequest } from '../types'

// Device hooks
export const useDevices = () => {
  return useQuery({
    queryKey: ['devices'],
    queryFn: deviceApi.getDevices,
  })
}

export const useDevice = (id: number) => {
  return useQuery({
    queryKey: ['device', id],
    queryFn: () => deviceApi.getDevice(id),
    enabled: !!id,
  })
}

export const useAgentDevices = () => {
  return useQuery({
    queryKey: ['devices', 'agent'],
    queryFn: deviceApi.getAgentDevices,
  })
}

export const useNetworkDevices = () => {
  return useQuery({
    queryKey: ['devices', 'network'],
    queryFn: deviceApi.getNetworkDevices,
  })
}

export const useAddDevice = () => {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: deviceApi.addDevice,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
    },
  })
}

export const useUpdateDevice = () => {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: ({ id, device }: { id: number; device: Partial<Device> }) =>
      deviceApi.updateDevice(id, device),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
    },
  })
}

export const useDeleteDevice = () => {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: deviceApi.deleteDevice,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['devices'] })
    },
  })
}

// Network scan hooks
export const useStartNetworkScan = () => {
  const queryClient = useQueryClient()
  
  return useMutation({
    mutationFn: (request: NetworkScanRequest) => networkApi.startScan(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['network-scan'] })
    },
  })
}

export const useNetworkScanResults = () => {
  return useQuery({
    queryKey: ['network-scan'],
    queryFn: networkApi.getScanResults,
  })
}

// Change logs hooks
export const useChangeLogs = () => {
  return useQuery({
    queryKey: ['change-logs'],
    queryFn: changeLogApi.getChangeLogs,
  })
}

export const useDeviceChangeLogs = (deviceId: number) => {
  return useQuery({
    queryKey: ['change-logs', 'device', deviceId],
    queryFn: () => changeLogApi.getDeviceChangeLogs(deviceId),
    enabled: !!deviceId,
  })
}

// Statistics hook
export const useStatistics = () => {
  return useQuery({
    queryKey: ['statistics'],
    queryFn: statisticsApi.getStatistics,
    refetchInterval: 30000, // Refresh every 30 seconds
  })
}